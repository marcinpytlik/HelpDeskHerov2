# HelpDeskHero Production Edition — Odcinek 3
## EF Core, AppDbContext i konfiguracje encji w osobnych klasach

## Tytuł roboczy odcinka

**HelpDeskHero — Odcinek 3: EF Core, DbContext i mapowanie domeny na SQL Server**

---

## Cel odcinka

W tym odcinku przechodzimy z modelu domenowego do warstwy infrastruktury.

W poprzednim odcinku utworzyliśmy encje:

```text
Tenant
OrganizationUnit
TicketType
WorkflowDefinition
WorkflowState
WorkflowTransition
Ticket
```

Teraz zaczynamy mapować ten model przez EF Core.

Celem odcinka jest:

- dodanie pakietów EF Core,
- przygotowanie `AppDbContext`,
- dodanie `DbSet`,
- utworzenie katalogu `Persistence`,
- utworzenie katalogu `Configurations`,
- przygotowanie osobnych konfiguracji encji,
- pokazanie `ApplyConfigurationsFromAssembly`,
- omówienie relacji między encjami,
- przygotowanie fundamentu pod pierwszą migrację.

---

# PROMPTER — wersja do czytania

Cześć.

W poprzednim odcinku rozpoczęliśmy budowę modelu domenowego HelpDeskHero.

Utworzyliśmy pierwsze encje: Tenant, OrganizationUnit, TicketType, WorkflowDefinition, WorkflowState, WorkflowTransition oraz Ticket.

Najważniejsza decyzja była taka, że status ticketu nie jest zwykłym stringiem. Aktualny stan zgłoszenia wynika z workflow, czyli z `WorkflowStateId`.

Dzisiaj idziemy krok dalej.

Będziemy mapować domenę na bazę danych przy pomocy EF Core.

Ale zrobimy to w taki sposób, żeby nie zamienić `DbContext` w jeden ogromny plik.

Nie będziemy wrzucać całej konfiguracji do `OnModelCreating`.

Zamiast tego każda encja dostanie osobną klasę konfiguracji.

To jest bardzo ważne w projekcie, który ma rosnąć.

---

## Dlaczego konfiguracje w osobnych klasach?

W bardzo małym projekcie można napisać konfigurację encji bezpośrednio w `OnModelCreating`.

Dla dwóch albo trzech encji to jeszcze jest akceptowalne.

Ale HelpDeskHero będzie miał wiele obszarów: tickety, komentarze, załączniki, workflow, tenantów, audyt, outbox, użytkowników, SLA, eskalacje, powiadomienia, webhooki i KPI.

Gdyby cała konfiguracja była w jednej metodzie, `OnModelCreating` bardzo szybko stałby się nieczytelny.

Dlatego przyjmujemy zasadę:

```text
jedna encja = jedna klasa konfiguracji EF Core
```

Dzięki temu łatwiej znaleźć konfigurację konkretnej encji, łatwiej zrobić code review, łatwiej dodawać indeksy i łatwiej kontrolować mapowanie SQL Server.

---

## Przypomnienie warstw

Encje domenowe są w projekcie:

```text
HelpDeskHero.Domain
```

Konfiguracje EF Core i `AppDbContext` będą w projekcie:

```text
HelpDeskHero.Infrastructure
```

Dlaczego?

Bo domena opisuje biznes.

A infrastruktura opisuje, jak ten biznes zapisujemy technicznie.

Domena nie musi wiedzieć, czy dane zapisujemy przez EF Core, Dapper, pliki JSON albo inny mechanizm.

W tym projekcie używamy EF Core i SQL Server, więc mapowanie trafia do `Infrastructure`.

---

# Krok 1 — dodanie pakietów EF Core

Z katalogu głównego solution dodajemy pakiety do projektu `Infrastructure`:

```powershell
dotnet add .\src\HelpDeskHero.Infrastructure\HelpDeskHero.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add .\src\HelpDeskHero.Infrastructure\HelpDeskHero.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add .\src\HelpDeskHero.Infrastructure\HelpDeskHero.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
```

Do projektu API dodajemy pakiet design, bo API będzie startup project dla migracji:

```powershell
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Microsoft.EntityFrameworkCore.Design
```

Sprawdzamy narzędzie EF Core:

```powershell
dotnet ef --version
```

Jeżeli go nie ma:

```powershell
dotnet tool install --global dotnet-ef
```

albo aktualizacja:

```powershell
dotnet tool update --global dotnet-ef
```

---

# Krok 2 — katalog Persistence

W projekcie `Infrastructure` tworzymy katalogi:

```powershell
mkdir .\src\HelpDeskHero.Infrastructure\Persistence
mkdir .\src\HelpDeskHero.Infrastructure\Persistence\Configurations
mkdir .\src\HelpDeskHero.Infrastructure\Persistence\Interceptors
mkdir .\src\HelpDeskHero.Infrastructure\Persistence\Seeding
mkdir .\src\HelpDeskHero.Infrastructure\DependencyInjection
```

Dzisiaj pracujemy głównie w:

```text
Persistence
Persistence/Configurations
DependencyInjection
```

---

# Krok 3 — AppDbContext

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/AppDbContext.cs
```

Kod:

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

Najważniejszy fragment to:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

Dzięki temu EF Core automatycznie znajdzie klasy implementujące `IEntityTypeConfiguration<T>` w projekcie `Infrastructure`.

---

# Krok 4 — TenantConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/TenantConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Tutaj ustawiamy tabelę, klucz główny, długości pól, wymagane pola, unikalność `Code` oraz `RowVersion`.

---

# Krok 5 — OrganizationUnitConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/OrganizationUnitConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("OrganizationUnits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.OrganizationUnits)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentOrganizationUnit)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentOrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Zwróć uwagę na `DeleteBehavior.Restrict`.

Nie chcemy przypadkowo skasować całej struktury organizacyjnej przez usunięcie rodzica albo tenanta.

---

# Krok 6 — TicketTypeConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/TicketTypeConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("TicketTypes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.TicketTypes)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Typ zgłoszenia jest unikalny w obrębie tenanta, dlatego indeks jest po `TenantId` i `Code`.

---

# Krok 7 — WorkflowDefinitionConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/WorkflowDefinitionConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.WorkflowDefinitions)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TicketType)
            .WithMany(x => x.WorkflowDefinitions)
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.TicketTypeId, x.Code })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Workflow jest związany z tenantem i typem ticketu.

Dzięki temu różne typy zgłoszeń mogą mieć różne procesy.

---

# Krok 8 — WorkflowStateConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/WorkflowStateConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowState>
{
    public void Configure(EntityTypeBuilder<WorkflowState> builder)
    {
        builder.ToTable("WorkflowStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.States)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.Code })
            .IsUnique();

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.SortOrder });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Stan workflow należy do definicji workflow.

Dla stanów workflow dopuszczamy cascade delete, ale w realnym systemie trzeba uważać, jeśli workflow jest już używany przez istniejące tickety.

---

# Krok 9 — WorkflowTransitionConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/WorkflowTransitionConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.Transitions)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FromState)
            .WithMany(x => x.OutgoingTransitions)
            .HasForeignKey(x => x.FromStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToState)
            .WithMany(x => x.IncomingTransitions)
            .HasForeignKey(x => x.ToStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.FromStateId, x.ToStateId })
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Tutaj mamy dwie relacje do tej samej tabeli `WorkflowStates`: `FromState` i `ToState`.

Dlatego konfigurujemy je jawnie i ustawiamy `DeleteBehavior.Restrict`, żeby uniknąć problemów z wieloma ścieżkami kaskadowego usuwania w SQL Server.

---

# Krok 10 — TicketConfiguration

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Configurations/TicketConfiguration.cs
```

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.AssignedToUserId)
            .HasMaxLength(450);

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TicketType)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkflowState)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.WorkflowStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Number })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.WorkflowStateId, x.CreatedAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.AssignedToUserId, x.CreatedAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.IsDeleted, x.CreatedAtUtc });

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
```

Najważniejsze decyzje:

- `Number` i `Title` są wymagane,
- `Priority` zapisujemy jako string,
- `TenantId + Number` jest unikalny,
- relacje mają `DeleteBehavior.Restrict`,
- dodajemy pierwsze indeksy pod typowe zapytania,
- konfigurujemy `RowVersion`.

---

# Po co indeksy już teraz?

W HelpDeskHero wiemy, jakie zapytania będą typowe.

Będziemy pobierać tickety:

- po tenancie,
- po stanie workflow,
- po osobie przypisanej,
- po dacie utworzenia,
- z pominięciem usuniętych logicznie.

Dlatego już teraz przygotowujemy pierwsze indeksy pod realne scenariusze.

EF Core i SQL Server to nie tylko tabele.

To też świadome projektowanie odczytów.

---

# Krok 11 — rejestracja DbContext w API

W projekcie `HelpDeskHero.Infrastructure` tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
```

```csharp
using HelpDeskHero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskHero.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
```

W `Program.cs` projektu API dodajemy:

```csharp
using HelpDeskHero.Infrastructure.DependencyInjection;

builder.Services.AddInfrastructure(builder.Configuration);
```

---

# Krok 12 — connection string developerski

W pliku:

```text
src/HelpDeskHero.Api/appsettings.Development.json
```

dodajemy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskHeroDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

To jest connection string developerski.

W kolejnych odcinkach pokażemy wersję produkcyjną z osobnym kontem migracyjnym i osobnym kontem runtime.

---

# Krok 13 — build projektu

Sprawdzamy build:

```powershell
dotnet build .\HelpDeskHero.sln
```

Jeżeli wszystko jest dobrze, solution powinno się zbudować.

Możliwe błędy na tym etapie to najczęściej:

- brak pakietu EF Core,
- brak referencji do `Infrastructure`,
- literówka w namespace,
- brak `using`,
- problem z relacją w konfiguracji.

---

# Krok 14 — pierwsza migracja

Jeżeli chcemy zakończyć odcinek pełnym przejściem do bazy, tworzymy pierwszą migrację:

```powershell
dotnet ef migrations add InitialCreate `
  --project .\src\HelpDeskHero.Infrastructure `
  --startup-project .\src\HelpDeskHero.Api `
  --context AppDbContext `
  --output-dir Persistence\Migrations
```

Po wygenerowaniu migracji warto otworzyć plik i zobaczyć, co EF Core przygotował.

Nie wykonujemy migracji bezrefleksyjnie.

Najpierw ją czytamy.

---

# Krok 15 — aktualizacja bazy developerskiej

Jeżeli migracja wygląda dobrze:

```powershell
dotnet ef database update `
  --project .\src\HelpDeskHero.Infrastructure `
  --startup-project .\src\HelpDeskHero.Api `
  --context AppDbContext
```

W development to jest w porządku.

W produkcji podejdziemy inaczej: idempotentny skrypt SQL, konto migracyjne i review.

---

# Co warto sprawdzić po migracji?

Po utworzeniu bazy sprawdzamy:

```text
Czy powstały tabele?
Czy są klucze główne?
Czy są klucze obce?
Czy są indeksy?
Czy RowVersion jest poprawnie skonfigurowany?
Czy enumy zapisują się jako string?
Czy relacje FromState i ToState nie powodują problemów?
```

To dobry moment, żeby pokazać, że EF Core generuje strukturę SQL Server, ale my musimy ją rozumieć.

---

# Ważne: EF Core nie zwalnia z myślenia o SQL Server

EF Core bardzo pomaga.

Ale EF Core nie zwalnia nas z myślenia o bazie danych.

Musimy rozumieć:

- relacje,
- klucze obce,
- indeksy,
- typy kolumn,
- zachowanie usuwania,
- concurrency,
- sposób generowania migracji.

To dlatego w tej serii cały czas łączymy EF Core z SQL Server.

Nie traktujemy bazy jako czarnej skrzynki.

---

# Podsumowanie

Podsumowując.

W tym odcinku przeszliśmy z modelu domenowego do EF Core.

Dodaliśmy:

```text
AppDbContext
DbSety
osobne klasy konfiguracji encji
relacje
indeksy
RowVersion
rejestrację DbContext w DI
connection string developerski
opcjonalnie pierwszą migrację
```

Najważniejsza decyzja:

```text
DbContext pozostaje krótki.
Każda encja ma osobną klasę konfiguracji.
EF Core mapuje domenę, ale nie miesza się z domeną.
```

W kolejnym odcinku możemy pójść w jednym z dwóch kierunków.

Albo rozwiniemy model o komentarze, załączniki, SLA i audyt.

Albo przejdziemy do bezpiecznego modelu migracji i kont SQL Server.

Na dziś mamy fundament EF Core gotowy.

Dzięki za uwagę i do zobaczenia w kolejnym materiale.

---

# Notatki dla prowadzącego

## Co pokazać na ekranie

- projekt `HelpDeskHero.Infrastructure`,
- katalog `Persistence`,
- katalog `Configurations`,
- `AppDbContext`,
- konfiguracje encji,
- `Program.cs`,
- `appsettings.Development.json`,
- komendę `dotnet build`,
- opcjonalnie komendę `dotnet ef migrations add`,
- opcjonalnie wygenerowaną migrację.

## Najważniejsze zdania

1. Domena opisuje biznes, Infrastructure opisuje technikalia.
2. `OnModelCreating` nie powinien być jednym wielkim plikiem.
3. Jedna encja powinna mieć jedną klasę konfiguracji.
4. EF Core nie zwalnia z myślenia o SQL Server.
5. `DeleteBehavior.Restrict` chroni przed przypadkowym kaskadowym usuwaniem.
6. `RowVersion` przygotowuje nas pod optimistic concurrency.
7. Migrację warto czytać przed wykonaniem.
