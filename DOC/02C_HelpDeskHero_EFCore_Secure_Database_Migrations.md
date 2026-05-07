# HelpDeskHero - EF Core + SQL Server: bezpieczny model tworzenia bazy, migracji i pracy aplikacji

## 1. Cel rozdziału

W wersji produkcyjnej **aplikacja nie powinna działać na koncie `sa`, `sysadmin`, `db_owner` ani na tym samym koncie, które wykonuje migracje EF Core**.

Chcemy pokazać bezpieczny i realistyczny model pracy:

1. **Administrator / DBA** zakłada bazę danych i konta logowania.
2. **Konto migracyjne** wykonuje migracje EF Core i zmiany schematu.
3. **Konto aplikacyjne** ma tylko uprawnienia potrzebne do codziennej pracy aplikacji.

Docelowy podział:

```text
SQL Server / DBA
 ├─ tworzy bazę HelpDeskHeroDb
 ├─ tworzy login hdh_migrator
 ├─ tworzy login hdh_app
 ├─ nadaje migratorowi uprawnienia do zmiany schematu
 └─ nadaje aplikacji ograniczone uprawnienia DML

EF Core migration pipeline
 └─ używa hdh_migrator

HelpDeskHero.Api runtime
 └─ używa hdh_app
```

To jest bardzo dobry temat do pokazania, bo wiele projektów EF Core robi błąd: aplikacja produkcyjna działa na koncie z uprawnieniami do `CREATE`, `ALTER`, `DROP`.

---

## 2. Zasada bezpieczeństwa

### Źle

```text
Aplikacja produkcyjna -> SQL login z db_owner
Aplikacja produkcyjna -> Database.Migrate()
Aplikacja produkcyjna -> automatycznie zmienia schemat przy starcie
```

Problem:

- błąd w aplikacji lub podatność może dać atakującemu uprawnienia do zmiany schematu,
- aplikacja może przypadkowo odpalić migrację w złym momencie,
- trudniej audytować, kto zmienił strukturę bazy,
- rollback i approval zmian są niekontrolowane.

### Dobrze

```text
DBA/admin -> tworzy bazę i konta
Migrator -> wykonuje migracje
Aplikacja -> tylko czyta i zapisuje dane biznesowe
```

Czyli:

```text
hdh_migrator != hdh_app
```

---

## 3. Role i konta

| Konto | Typ | Cel | Przykładowe uprawnienia |
|---|---|---|---|
| Administrator / DBA | konto osobowe lub serwisowe administracyjne | zakładanie bazy, loginów, awaryjne operacje | `sysadmin` lub kontrolowane uprawnienia administracyjne |
| `hdh_migrator` | login SQL / konto techniczne | migracje EF Core | `db_ddladmin`, `db_datareader`, `db_datawriter`, ewentualnie dodatkowe `ALTER` na schematach |
| `hdh_app` | login SQL / konto aplikacyjne | normalna praca aplikacji | `db_datareader`, `db_datawriter`, `EXECUTE` na schemacie API, bez `db_ddladmin`, bez `db_owner` |

> W środowisku domenowym możesz zamiast loginów SQL użyć kont domenowych, np. `DOMENA\svc-hdh-migrator` i `DOMENA\svc-hdh-app`. Wtedy używasz `CREATE LOGIN [DOMENA\svc-hdh-app] FROM WINDOWS`.

---

## 4. Skrypt DBA - utworzenie bazy i loginów SQL

Plik przykładowy:

```text
sql/00_admin_create_database_and_logins.sql
```

Uruchamia DBA/admin na SQL Server.

```sql
USE [master];
GO

IF DB_ID(N'HelpDeskHeroDb') IS NULL
BEGIN
    CREATE DATABASE [HelpDeskHeroDb];
END
GO

-- Login do migracji EF Core
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'hdh_migrator')
BEGIN
    CREATE LOGIN [hdh_migrator]
    WITH PASSWORD = 'CHANGE_ME_Strong_Migrator_Password_2026!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = ON;
END
GO

-- Login aplikacyjny runtime
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'hdh_app')
BEGIN
    CREATE LOGIN [hdh_app]
    WITH PASSWORD = 'CHANGE_ME_Strong_App_Password_2026!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = ON;
END
GO

USE [HelpDeskHeroDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'hdh_migrator')
BEGIN
    CREATE USER [hdh_migrator] FOR LOGIN [hdh_migrator];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'hdh_app')
BEGIN
    CREATE USER [hdh_app] FOR LOGIN [hdh_app];
END
GO
```

---

## 5. Uprawnienia dla konta migracyjnego

Konto migracyjne musi móc:

- tworzyć i zmieniać tabele,
- tworzyć indeksy,
- tworzyć klucze obce,
- modyfikować schemat,
- zapisywać do tabeli `__EFMigrationsHistory`,
- opcjonalnie seedować dane startowe.

Najprostszy wariant edukacyjny:

```sql
USE [HelpDeskHeroDb];
GO

ALTER ROLE [db_ddladmin] ADD MEMBER [hdh_migrator];
ALTER ROLE [db_datareader] ADD MEMBER [hdh_migrator];
ALTER ROLE [db_datawriter] ADD MEMBER [hdh_migrator];
GO
```

To nie jest `db_owner`, ale nadal daje szerokie uprawnienia do DDL. Dla kursu i projektu produkcyjnego jest to dobry kompromis do pokazania różnicy między kontem migracyjnym a kontem aplikacyjnym.

### Wariant bardziej restrykcyjny

Jeśli chcesz mocniej ograniczać migratora, możesz pracować na dedykowanych schematach i nadawać uprawnienia na schematy:

```sql
USE [HelpDeskHeroDb];
GO

CREATE SCHEMA [app] AUTHORIZATION [dbo];
GO

GRANT ALTER ON SCHEMA::[app] TO [hdh_migrator];
GRANT CONTROL ON SCHEMA::[app] TO [hdh_migrator];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[app] TO [hdh_migrator];
GO
```

Uwaga: EF Core migrations często wykonują operacje, które mogą wymagać szerszych praw niż czyste DML. Dlatego w praktyce często stosuje się dedykowane konto migracyjne z podwyższonymi uprawnieniami, ale używane tylko w pipeline migracyjnym, nie w runtime aplikacji.

---

## 6. Uprawnienia dla konta aplikacyjnego

Konto aplikacyjne nie powinno zmieniać schematu.

Minimalny wariant dla aplikacji EF Core pracującej bez procedur składowanych:

```sql
USE [HelpDeskHeroDb];
GO

ALTER ROLE [db_datareader] ADD MEMBER [hdh_app];
ALTER ROLE [db_datawriter] ADD MEMBER [hdh_app];
GO
```

Wariant bardziej kontrolowany, z dedykowanym schematem `app`:

```sql
USE [HelpDeskHeroDb];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[app] TO [hdh_app];
GO
```

Jeżeli używasz procedur składowanych, dodatkowo:

```sql
GRANT EXECUTE ON SCHEMA::[app] TO [hdh_app];
GO
```

Czego **nie** nadajemy aplikacji:

```sql
-- NIE dla konta aplikacyjnego:
-- ALTER ROLE [db_owner] ADD MEMBER [hdh_app];
-- ALTER ROLE [db_ddladmin] ADD MEMBER [hdh_app];
-- GRANT ALTER TO [hdh_app];
-- GRANT CONTROL TO [hdh_app];
```

---

## 7. Schematy SQL Server dla HelpDeskHero

Dla czytelności produkcyjnej warto nie trzymać wszystkiego w `dbo`.

Proponowany podział:

| Schemat | Zawartość |
|---|---|
| `app` | główne tabele biznesowe: tickets, workflow, tenants, knowledge base |
| `auth` | tabele Identity, refresh tokeny |
| `audit` | audit log |
| `ops` | outbox, KPI, job/status tables |

Przykładowy skrypt:

```sql
USE [HelpDeskHeroDb];
GO

IF SCHEMA_ID(N'app') IS NULL EXEC(N'CREATE SCHEMA [app] AUTHORIZATION [dbo]');
IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth] AUTHORIZATION [dbo]');
IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit] AUTHORIZATION [dbo]');
IF SCHEMA_ID(N'ops') IS NULL EXEC(N'CREATE SCHEMA [ops] AUTHORIZATION [dbo]');
GO
```

Uprawnienia runtime:

```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[app] TO [hdh_app];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[auth] TO [hdh_app];
GRANT INSERT ON SCHEMA::[audit] TO [hdh_app];
GRANT SELECT ON SCHEMA::[audit] TO [hdh_app];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[ops] TO [hdh_app];
GO
```

Dla `audit` można rozważyć bardziej restrykcyjny wariant: aplikacja może tylko `INSERT`, a odczyt logów audytowych ma osobny endpoint administracyjny lub osobne konto raportowe.

---

## 8. EF Core - mapowanie tabel do schematów

W `AppDbContext` konfigurujemy schematy jawnie.

Przykład:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.HasDefaultSchema("app");

    modelBuilder.Entity<Tenant>(b =>
    {
        b.ToTable("Tenants", "app");
        b.HasKey(x => x.Id);
    });

    modelBuilder.Entity<OrganizationUnit>(b =>
    {
        b.ToTable("OrganizationUnits", "app");
        b.HasKey(x => x.Id);
    });

    modelBuilder.Entity<Ticket>(b =>
    {
        b.ToTable("Tickets", "app");
        b.HasKey(x => x.Id);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantProvider.TenantId);
    });

    modelBuilder.Entity<AuditLog>(b =>
    {
        b.ToTable("AuditLogs", "audit");
        b.HasKey(x => x.Id);
    });

    modelBuilder.Entity<OutboxMessage>(b =>
    {
        b.ToTable("OutboxMessages", "ops");
        b.HasKey(x => x.Id);
    });
}
```

Dla ASP.NET Identity można przenieść tabele do schematu `auth`:

```csharp
modelBuilder.Entity<ApplicationUser>().ToTable("Users", "auth");
modelBuilder.Entity<IdentityRole>().ToTable("Roles", "auth");
modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "auth");
modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "auth");
modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "auth");
modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "auth");
modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "auth");
```

Wymagane usingi:

```csharp
using Microsoft.AspNetCore.Identity;
```

---

## 9. Dwa connection stringi

W aplikacji rozdzielamy connection string runtime i migracyjny.

### `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskHeroDb;User Id=hdh_app;Password=CHANGE_ME_Strong_App_Password_2026!;TrustServerCertificate=True;",
    "MigrationConnection": "Server=localhost;Database=HelpDeskHeroDb;User Id=hdh_migrator;Password=CHANGE_ME_Strong_Migrator_Password_2026!;TrustServerCertificate=True;"
  }
}
```

W produkcji nie trzymamy haseł w pliku. Używamy:

- zmiennych środowiskowych,
- secret store dla developmentu,
- Azure Key Vault / innego vaulta,
- mechanizmu CI/CD secrets.

---

## 10. Program.cs - runtime używa tylko konta aplikacyjnego

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing DefaultConnection.");

    options.UseSqlServer(connectionString);

    var interceptors = sp.GetServices<IInterceptor>();
    options.AddInterceptors(interceptors);
});
```

Runtime API korzysta z `DefaultConnection`, czyli z konta `hdh_app`.

---

## 11. Design-time factory dla migracji EF Core

Aby migracje używały osobnego konta, dodajemy `IDesignTimeDbContextFactory`.

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/AppDbContextFactory.cs
```

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HelpDeskHero.Api.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("MigrationConnection")
            ?? throw new InvalidOperationException("Missing MigrationConnection.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
```

Jeżeli `AppDbContext` wymaga np. `ITenantProvider`, przygotuj wersję design-time providera:

```csharp
public sealed class DesignTimeTenantProvider : ITenantProvider
{
    public Guid TenantId => Guid.Empty;
    public string? TenantCode => "design-time";
}
```

Wtedy fabryka musi utworzyć kontekst z wymaganymi zależnościami albo kontekst powinien przyjmować zależności opcjonalnie dla design-time. Najczystszy wariant: query filtery bazujące na tenant providerze projektujemy tak, aby migracje nie wymagały realnego żądania HTTP.

---

## 12. Wykonywanie migracji przez konto migracyjne

### Dodanie migracji

```powershell
dotnet ef migrations add InitialProductionSchema `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --context AppDbContext
```

### Wykonanie migracji na bazie

```powershell
dotnet ef database update `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --context AppDbContext
```

Dzięki `AppDbContextFactory` komenda użyje `MigrationConnection`, czyli konta `hdh_migrator`.

---

## 13. Lepszy wariant produkcyjny: generowanie skryptu SQL

W produkcji często lepiej nie odpalać `dotnet ef database update` bezpośrednio, tylko wygenerować skrypt SQL i dać go do review DBA.

```powershell
dotnet ef migrations script `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --context AppDbContext `
  --idempotent `
  --output .\artifacts\sql\HelpDeskHeroDb_migration.sql
```

Wtedy proces wygląda tak:

```text
Developer -> tworzy migrację EF Core
CI/CD -> generuje idempotentny skrypt SQL
DBA / pipeline migracyjny -> wykonuje skrypt kontem hdh_migrator
Aplikacja -> działa kontem hdh_app
```

To jest najlepszy wariant do pokazania w materiale o bezpiecznym EF Core.

---

## 14. Zakaz automatycznego `Database.Migrate()` w runtime

W aplikacji produkcyjnej nie robimy:

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
```

To może być dopuszczalne w lokalnym demo, ale nie w produkcji.

Zamiast tego:

```text
migracje są etapem deploymentu, a nie skutkiem ubocznym startu aplikacji
```

Jeśli chcesz mieć zabezpieczenie, możesz dodać health check sprawdzający, czy baza jest zgodna z oczekiwaną migracją.

---

## 15. Health check migracji

Można sprawdzić ostatnią zastosowaną migrację.

Przykład serwisu diagnostycznego:

```csharp
public sealed class DatabaseMigrationStatusService
{
    private readonly AppDbContext _db;

    public DatabaseMigrationStatusService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken ct)
    {
        var migrations = await _db.Database.GetAppliedMigrationsAsync(ct);
        return migrations.ToList();
    }
}
```

Do pełnej kontroli możesz porównać:

```csharp
var applied = await db.Database.GetAppliedMigrationsAsync(ct);
var pending = await db.Database.GetPendingMigrationsAsync(ct);
```

A endpoint administracyjny `/api/admin/database/migrations` może pokazać, czy są pending migrations. Nie powinien ich wykonywać.

---

## 16. Test bezpieczeństwa uprawnień

Po wdrożeniu sprawdź, że konto aplikacyjne nie może zmienić schematu.

Zaloguj się jako `hdh_app` i wykonaj:

```sql
CREATE TABLE app.ShouldFail
(
    Id int NOT NULL
);
```

Oczekiwany wynik:

```text
The CREATE TABLE permission was denied in database 'HelpDeskHeroDb'.
```

Sprawdź, że aplikacja może wykonywać DML:

```sql
SELECT TOP (10) * FROM app.Tickets;
```

oraz po utworzeniu testowego ticketu przez API sprawdź, czy rekord pojawia się w tabeli.

---

## 17. Skrypt weryfikacyjny DBA

```sql
USE [HelpDeskHeroDb];
GO

SELECT
    dp.name AS DatabaseUser,
    dp.type_desc AS UserType,
    rp.name AS DatabaseRole
FROM sys.database_role_members drm
JOIN sys.database_principals rp
    ON drm.role_principal_id = rp.principal_id
JOIN sys.database_principals dp
    ON drm.member_principal_id = dp.principal_id
WHERE dp.name IN (N'hdh_migrator', N'hdh_app')
ORDER BY dp.name, rp.name;
GO

SELECT
    pr.state_desc,
    pr.permission_name,
    USER_NAME(pr.grantee_principal_id) AS Grantee,
    OBJECT_SCHEMA_NAME(pr.major_id) AS ObjectSchema,
    OBJECT_NAME(pr.major_id) AS ObjectName
FROM sys.database_permissions pr
WHERE USER_NAME(pr.grantee_principal_id) IN (N'hdh_migrator', N'hdh_app')
ORDER BY Grantee, permission_name;
GO
```

---

## 18. GitHub Actions / pipeline - idea

W pipeline produkcyjnym możesz mieć dwa kroki:

```text
1. Build aplikacji
2. Generate migration SQL artifact
3. Approval / DBA review
4. Execute migration script using hdh_migrator
5. Deploy API using hdh_app connection string
```

Przykładowy krok generowania skryptu:

```yaml
- name: Generate EF Core migration script
  run: |
    dotnet tool restore
    dotnet ef migrations script \
      --project ./src/HelpDeskHero.Api/HelpDeskHero.Api.csproj \
      --startup-project ./src/HelpDeskHero.Api/HelpDeskHero.Api.csproj \
      --context AppDbContext \
      --idempotent \
      --output ./artifacts/sql/HelpDeskHeroDb_migration.sql
```

Wykonanie skryptu powinno używać sekretu z connection stringiem migracyjnym, nie runtime connection stringiem.

---

## 19. Checklist produkcyjny

Przed uznaniem środowiska za poprawnie przygotowane:

- [ ] Baza została utworzona przez DBA/admina.
- [ ] Istnieje osobne konto migracyjne.
- [ ] Istnieje osobne konto aplikacyjne.
- [ ] Konto aplikacyjne nie ma `db_owner`.
- [ ] Konto aplikacyjne nie ma `db_ddladmin`.
- [ ] Konto aplikacyjne nie może wykonywać `CREATE TABLE`.
- [ ] Konto migracyjne może wykonać migracje EF Core.
- [ ] Runtime API używa `DefaultConnection`.
- [ ] Migracje używają `MigrationConnection`.
- [ ] Produkcja nie wykonuje `Database.Migrate()` przy starcie aplikacji.
- [ ] Skrypty migracyjne mogą być generowane jako artefakt CI/CD.
- [ ] Hasła nie są przechowywane w repozytorium.
- [ ] Uprawnienia można zweryfikować skryptem DBA.

---

## 20. Przekaz do nagrania

Najważniejszy komunikat:

> EF Core nie wymaga, żeby aplikacja produkcyjna była właścicielem bazy. Możemy bezpiecznie rozdzielić obowiązki: administrator zakłada bazę, konto migracyjne zmienia schemat, a konto aplikacyjne tylko pracuje na danych. To jest dużo zdrowszy model niż `db_owner` w connection stringu aplikacji.

