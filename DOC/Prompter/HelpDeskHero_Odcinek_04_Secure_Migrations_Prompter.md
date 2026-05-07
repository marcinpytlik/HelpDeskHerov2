# HelpDeskHero Production Edition — Odcinek 4
## Bezpieczne migracje EF Core i rozdzielenie kont SQL Server

## Tytuł roboczy odcinka

**HelpDeskHero — Odcinek 4: EF Core bez db_owner, czyli bezpieczne migracje i konta SQL Server**

---

## Cel odcinka

W tym odcinku pokazujemy bardzo ważny element produkcyjnego podejścia do EF Core i SQL Server.

W poprzednim odcinku utworzyliśmy `AppDbContext`, `DbSety`, osobne konfiguracje encji, rejestrację DbContext w DI i przygotowaliśmy grunt pod migracje.

Teraz odpowiadamy na pytanie:

> Jak robić migracje EF Core bez dawania aplikacji uprawnień `db_owner`?

Celem odcinka jest pokazanie trzech odpowiedzialności:

```text
Administrator / DBA  -> tworzy bazę, loginy, użytkowników i role
Konto migracyjne     -> wykonuje migracje EF Core i seed
Konto runtime        -> działa w aplikacji na minimalnych uprawnieniach
```

---

# PROMPTER — wersja do czytania

Cześć.

W poprzednim odcinku zrobiliśmy pierwszy realny most między domeną a SQL Server.

Dodaliśmy EF Core, `AppDbContext`, `DbSety` oraz osobne konfiguracje encji.

Dzisiaj przechodzimy do tematu, który jest bardzo ważny w projektach produkcyjnych, a bardzo często jest pomijany w prostych tutorialach.

Chodzi o bezpieczne migracje EF Core i uprawnienia do SQL Server.

Bardzo często widzimy podejście, w którym aplikacja ma szerokie uprawnienia do bazy i przy starcie wykonuje `Database.Migrate()`.

W development to może być wygodne.

Ale w produkcji nie chciałbym, żeby konto aplikacji runtime było właścicielem bazy tylko po to, żeby mogło wykonać migrację.

Najważniejsza myśl tego odcinka brzmi:

```text
EF Core nie wymaga, żeby aplikacja runtime miała db_owner.
```

To my decydujemy, jakiego konta używamy do migracji, a jakiego do codziennej pracy aplikacji.

---

## Główna idea

W HelpDeskHero przyjmujemy trzy odpowiedzialności.

Pierwsza odpowiedzialność to administrator albo DBA.

Administrator tworzy bazę danych, loginy, użytkowników, role i nadaje uprawnienia.

Druga odpowiedzialność to konto migracyjne.

To konto wykonuje migracje EF Core, zmiany schematu i seed danych startowych.

Trzecia odpowiedzialność to konto runtime aplikacji.

To konto jest używane przez API na co dzień.

I to konto nie powinno mieć `db_owner`.

Aplikacja ma pracować z danymi, a nie dowolnie zmieniać strukturę bazy.

---

## Nazwy używane w demo

W tym odcinku użyjemy przykładowych nazw:

```text
HelpDeskHeroDb
hdh_migrator
hdh_app
role_helpdeskhero_app
```

`HelpDeskHeroDb` to baza danych.

`hdh_migrator` to konto do migracji.

`hdh_app` to konto runtime aplikacji.

`role_helpdeskhero_app` to rola bazodanowa dla aplikacji.

---

# Krok 1 — skrypt administracyjny

Tworzymy plik:

```text
scripts/sql/00_create_database_and_security.sql
```

Ten skrypt wykonuje administrator SQL Server.

```sql
USE master;
GO

IF DB_ID(N'HelpDeskHeroDb') IS NULL
BEGIN
    CREATE DATABASE HelpDeskHeroDb;
END
GO

IF SUSER_ID(N'hdh_migrator') IS NULL
BEGIN
    CREATE LOGIN hdh_migrator
    WITH PASSWORD = 'Change_Me_Strong_Migrator_Password_123!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

IF SUSER_ID(N'hdh_app') IS NULL
BEGIN
    CREATE LOGIN hdh_app
    WITH PASSWORD = 'Change_Me_Strong_App_Password_123!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

USE HelpDeskHeroDb;
GO

IF USER_ID(N'hdh_migrator') IS NULL
BEGIN
    CREATE USER hdh_migrator FOR LOGIN hdh_migrator;
END
GO

IF USER_ID(N'hdh_app') IS NULL
BEGIN
    CREATE USER hdh_app FOR LOGIN hdh_app;
END
GO
```

W prawdziwym środowisku haseł nie trzymamy w repozytorium.

Tutaj pokazujemy mechanizm w demo.

---

# Krok 2 — uprawnienia migratora

Konto migracyjne musi tworzyć i zmieniać schemat.

W wariancie demonstracyjnym nadajemy mu `db_owner`.

```sql
USE HelpDeskHeroDb;
GO

IF IS_ROLEMEMBER(N'db_owner', N'hdh_migrator') = 0
BEGIN
    ALTER ROLE db_owner ADD MEMBER hdh_migrator;
END
GO
```

Bardzo ważne: to nie jest konto runtime aplikacji.

To konto jest używane tylko do migracji i seeda.

---

# Krok 3 — rola runtime aplikacji

Teraz tworzymy rolę aplikacyjną.

```sql
USE HelpDeskHeroDb;
GO

IF DATABASE_PRINCIPAL_ID(N'role_helpdeskhero_app') IS NULL
BEGIN
    CREATE ROLE role_helpdeskhero_app;
END
GO

IF IS_ROLEMEMBER(N'role_helpdeskhero_app', N'hdh_app') = 0
BEGIN
    ALTER ROLE role_helpdeskhero_app ADD MEMBER hdh_app;
END
GO
```

---

# Krok 4 — minimalne uprawnienia runtime

Po migracji można nadać uprawnienia na schemat `dbo`.

```sql
USE HelpDeskHeroDb;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO role_helpdeskhero_app;
GO
```

To nadal jest dość szerokie uprawnienie, ale dużo mniejsze niż `db_owner`.

Aplikacja może pracować z danymi, ale nie jest właścicielem bazy.

W bardziej restrykcyjnym modelu można nadać uprawnienia do konkretnych tabel.

Na potrzeby demo pokazujemy zasadę:

```text
konto migracyjne != konto runtime aplikacji
```

---

# Krok 5 — connection stringi

W pliku:

```text
src/HelpDeskHero.Api/appsettings.Development.json
```

dodajemy dwa connection stringi:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskHeroDb;User Id=hdh_app;Password=Change_Me_Strong_App_Password_123!;TrustServerCertificate=True;",
    "MigrationConnection": "Server=localhost;Database=HelpDeskHeroDb;User Id=hdh_migrator;Password=Change_Me_Strong_Migrator_Password_123!;TrustServerCertificate=True;"
  }
}
```

`DefaultConnection` to konto aplikacji.

`MigrationConnection` to konto migratora.

W produkcji hasła powinny być w bezpiecznym miejscu, na przykład w zmiennych środowiskowych, secret managerze albo firmowym sejfie haseł.

---

# Krok 6 — DesignTimeDbContextFactory

Żeby `dotnet ef` używał connection stringa migracyjnego, tworzymy fabrykę.

Plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/AppDbContextFactory.cs
```

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HelpDeskHero.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "HelpDeskHero.Api");

        if (!Directory.Exists(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "HelpDeskHero.Api");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("MigrationConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
```

Ta fabryka jest używana przez narzędzia EF Core.

Dzięki temu migracje idą przez `MigrationConnection`.

Runtime aplikacji nadal używa `DefaultConnection`.

---

# Krok 7 — runtime używa DefaultConnection

W `InfrastructureServiceCollectionExtensions` zostawiamy:

```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection");

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Czyli API działa jako `hdh_app`.

To jest dokładnie to, czego chcemy.

---

# Krok 8 — utworzenie migracji

Jeżeli nie mamy jeszcze migracji:

```powershell
dotnet ef migrations add InitialCreate `
  --project .\\src\\HelpDeskHero.Infrastructure `
  --startup-project .\\src\\HelpDeskHero.Api `
  --context AppDbContext `
  --output-dir Persistence\\Migrations
```

Po utworzeniu migracji warto ją przeczytać.

Sprawdzamy:

```text
tabele
klucze główne
klucze obce
indeksy
RowVersion
relacje WorkflowState From/To
```

Migracja nie jest magicznym plikiem. Trzeba ją rozumieć.

---

# Krok 9 — idempotentny skrypt SQL

W produkcji często lepsze jest wygenerowanie skryptu SQL:

```powershell
dotnet ef migrations script `
  --idempotent `
  --project .\\src\\HelpDeskHero.Infrastructure `
  --startup-project .\\src\\HelpDeskHero.Api `
  --context AppDbContext `
  --output .\\scripts\\sql\\migrations\\HelpDeskHeroDb_InitialCreate.sql
```

Taki skrypt może:

```text
przejść review DBA,
być artefaktem release,
zostać wykonany kontrolowanie,
trafić do historii wdrożenia.
```

To jest bliższe produkcyjnemu podejściu niż automatyczne migrowanie bazy przy starcie API.

---

# Krok 10 — wykonanie migracji kontem migratora

W development możemy wykonać:

```powershell
dotnet ef database update `
  --project .\\src\\HelpDeskHero.Infrastructure `
  --startup-project .\\src\\HelpDeskHero.Api `
  --context AppDbContext
```

Dzięki `AppDbContextFactory` narzędzie EF Core użyje `MigrationConnection`, jeśli jest dostępny.

Czyli migracja pójdzie kontem `hdh_migrator`.

---

# Krok 11 — test runtime kontem aplikacji

Po migracji uruchamiamy API:

```powershell
dotnet run --project .\\src\\HelpDeskHero.Api
```

API użyje `DefaultConnection`, czyli konta `hdh_app`.

Jeżeli pojawi się błąd uprawnień, to znaczy, że trzeba sprawdzić, czego aplikacja faktycznie potrzebuje.

Minimalne uprawnienia trzeba testować.

---

# Czego konto runtime nie powinno robić?

Konto `hdh_app` nie powinno:

```text
tworzyć tabel,
usuwać tabel,
zmieniać schematu,
wykonywać migracji,
tworzyć loginów,
zarządzać rolami,
mieć db_owner,
mieć sysadmin.
```

Konto runtime powinno obsługiwać codzienną pracę aplikacji.

---

# Database.Migrate przy starcie aplikacji

W wielu tutorialach spotkamy:

```csharp
await db.Database.MigrateAsync();
```

wykonywane przy starcie aplikacji.

W development jest to wygodne.

W production nie traktowałbym tego jako domyślnego modelu.

Dlaczego?

Bo aplikacja musi mieć uprawnienia do zmiany schematu.

Bo kilka instancji aplikacji może wystartować równocześnie.

Bo migracja może się nie udać podczas startu.

Bo zmiana schematu powinna być częścią kontrolowanego procesu wdrożeniowego.

Dlatego w HelpDeskHero nie uruchamiamy migracji automatycznie przy starcie API w production.

---

# Checklist po odcinku

Po tym odcinku powinniśmy mieć:

```text
bazę HelpDeskHeroDb,
login hdh_migrator,
login hdh_app,
użytkownika hdh_migrator,
użytkownika hdh_app,
rolę role_helpdeskhero_app,
uprawnienia runtime dla hdh_app,
MigrationConnection,
DefaultConnection,
AppDbContextFactory,
migrację EF Core,
opcjonalnie idempotentny skrypt SQL.
```

---

# Podsumowanie

Podsumowując.

W tym odcinku pokazaliśmy bezpieczniejszy model pracy z EF Core i SQL Server.

Rozdzieliliśmy odpowiedzialności:

```text
administrator tworzy bazę i konta,
migrator wykonuje migracje,
aplikacja działa na ograniczonym koncie.
```

Najważniejsza lekcja:

```text
EF Core nie wymaga, żeby aplikacja runtime miała db_owner.
```

W kolejnym odcinku przejdziemy do seeda danych produkcyjnych.

Pokażemy, jak przygotować seed w osobnych klasach, żeby nie robić jednego wielkiego `DbInitializer`.

Dzięki za uwagę i do zobaczenia w kolejnym materiale.

---

# Krótsze zakończenie

Na dziś tyle.

Konto aplikacji runtime nie musi mieć `db_owner`.

Migracje wykonuje konto migracyjne, a aplikacja działa na ograniczonych uprawnieniach.

W kolejnym odcinku przejdziemy do seeda danych i przygotujemy pierwsze dane startowe: tenant, jednostki organizacyjne, typy ticketów i workflow.

Dzięki za uwagę.

---

# Notatki dla prowadzącego

## Co pokazać na ekranie

- VS Code,
- `scripts/sql/00_create_database_and_security.sql`,
- `appsettings.Development.json`,
- `AppDbContextFactory`,
- `InfrastructureServiceCollectionExtensions`,
- PowerShell z komendami `dotnet ef`,
- SQL Server z bazą i użytkownikami,
- opcjonalnie wygenerowany skrypt migracji.

## Najważniejsze zdania

1. EF Core nie wymaga `db_owner` dla konta aplikacji runtime.
2. Konto migracyjne i konto aplikacji to dwa różne konta.
3. `Database.Migrate()` przy starcie API jest wygodne w dev, ale ryzykowne w production.
4. Idempotentny skrypt SQL może przejść review DBA.
5. Minimalne uprawnienia trzeba testować.
6. Aplikacja ma pracować z danymi, a nie zarządzać schematem bazy.
