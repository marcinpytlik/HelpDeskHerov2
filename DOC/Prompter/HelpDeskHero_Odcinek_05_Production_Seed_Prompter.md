# HelpDeskHero Production Edition — Odcinek 5
## Seed danych produkcyjnych w osobnych klasach

## Tytuł roboczy odcinka

**HelpDeskHero — Odcinek 5: seed danych w EF Core bez wielkiego DbInitializera**

---

## Cel odcinka

W poprzednim odcinku pokazaliśmy bezpieczne migracje EF Core i rozdzielenie kont SQL Server:

```text
administrator / DBA
konto migracyjne
konto runtime aplikacji
```

Pokazaliśmy też, że aplikacja runtime nie musi mieć `db_owner`.

W tym odcinku przechodzimy do kolejnego ważnego elementu: **seed danych startowych**.

Celem odcinka jest pokazanie:

- dlaczego seed danych jest potrzebny,
- dlaczego jeden wielki `DbInitializer` szybko robi się problemem,
- jak podzielić seed na osobne klasy,
- jak przygotować interfejs `ISeedStep`,
- jak przygotować `DatabaseSeeder`,
- jak dodać pierwszego tenanta,
- jak dodać jednostki organizacyjne,
- jak dodać typy ticketów,
- jak dodać workflow,
- jak zadbać o idempotencję,
- dlaczego seed powinien być uruchamiany kontrolowanie, najlepiej przez konto migracyjne.

---

# PROMPTER — wersja do czytania

Cześć.

W poprzednim odcinku pokazaliśmy, jak podejść do migracji EF Core w bardziej produkcyjny sposób.

Rozdzieliliśmy konto migracyjne od konta runtime aplikacji.

Najważniejsza myśl była taka:

```text
EF Core nie wymaga, żeby aplikacja runtime miała db_owner.
```

Dzisiaj idziemy krok dalej.

Skoro mamy bazę danych i migracje, to aplikacja potrzebuje jeszcze danych startowych.

I właśnie o tym będzie ten odcinek.

Będziemy robić seed danych.

Ale nie w formie jednego ogromnego `DbInitializer`, do którego z czasem trafia wszystko.

Zrobimy to w sposób bardziej uporządkowany.

Każdy obszar dostanie osobną klasę seedującą.

---

## Dlaczego seed danych jest potrzebny?

W HelpDeskHero nie wszystko może być puste po starcie aplikacji.

Potrzebujemy danych startowych, żeby system mógł działać.

Na przykład:

```text
tenant demonstracyjny,
jednostki organizacyjne,
typy ticketów,
workflow,
stany workflow,
przejścia workflow,
opcjonalnie role,
opcjonalnie konto administratora,
opcjonalnie dane demonstracyjne.
```

Bez tych danych trudno utworzyć pierwszy ticket.

Bo ticket potrzebuje tenanta.

Ticket może potrzebować jednostki organizacyjnej.

Ticket potrzebuje typu.

Ticket potrzebuje początkowego stanu workflow.

Czyli zanim użytkownik zacznie pracę, pewna minimalna konfiguracja musi już istnieć.

---

## Problem z jednym DbInitializer

W małych projektach często widzimy coś takiego:

```text
DbInitializer.cs
```

A w nim wszystko:

```text
role,
użytkownicy,
tenant,
typy ticketów,
workflow,
tickety testowe,
komentarze,
załączniki,
ustawienia,
raporty,
wszystko.
```

Na początku to jest wygodne.

Ale z czasem ten plik robi się ogromny.

Trudno go czytać.

Trudno go testować.

Trudno zmieniać.

Trudno powiedzieć, który fragment odpowiada za który obszar.

Dlatego w HelpDeskHero przyjmujemy inną zasadę:

```text
jeden obszar = jeden seed step
```

Czyli osobny krok dla tenantów, osobny dla jednostek organizacyjnych, osobny dla typów ticketów, osobny dla workflow i tak dalej.

---

## Docelowa struktura

W projekcie `Infrastructure` pracujemy w katalogu:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding
```

Docelowo chcemy mieć strukturę:

```text
Persistence/Seeding
├── ISeedStep.cs
├── IDatabaseSeeder.cs
├── DatabaseSeeder.cs
├── TenantSeedStep.cs
├── OrganizationUnitSeedStep.cs
├── TicketTypeSeedStep.cs
├── WorkflowSeedStep.cs
└── DemoTicketSeedStep.cs
```

Na tym etapie skupimy się na podstawie:

```text
TenantSeedStep
OrganizationUnitSeedStep
TicketTypeSeedStep
WorkflowSeedStep
```

Dane demonstracyjne ticketów zostawimy na później.

---

# Krok 1 — ISeedStep

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/ISeedStep.cs
```

Kod:

```csharp
namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public interface ISeedStep
{
    int Order { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
```

`Order` pozwala określić kolejność wykonywania kroków.

To jest ważne, bo seed ma zależności.

Najpierw musi powstać tenant.

Potem jednostki organizacyjne i typy ticketów.

Potem workflow, który zależy od typów ticketów.

---

# Krok 2 — IDatabaseSeeder

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/IDatabaseSeeder.cs
```

Kod:

```csharp
namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
```

To jest prosty kontrakt dla seeda całej bazy.

---

# Krok 3 — DatabaseSeeder

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/DatabaseSeeder.cs
```

Kod:

```csharp
namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IEnumerable<ISeedStep> _steps;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IEnumerable<ISeedStep> steps,
        ILogger<DatabaseSeeder> logger)
    {
        _steps = steps;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var step in _steps.OrderBy(x => x.Order))
        {
            _logger.LogInformation(
                "Running seed step {SeedStep}",
                step.GetType().Name);

            await step.ExecuteAsync(cancellationToken);
        }
    }
}
```

Ten obiekt nie wie, co dokładnie seedujemy.

On tylko uruchamia kroki w odpowiedniej kolejności.

To jest bardzo wygodne, bo możemy dodawać kolejne seed stepy bez rozbudowywania jednego wielkiego pliku.

---

# Krok 4 — TenantSeedStep

Zaczynamy od tenanta.

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/TenantSeedStep.cs
```

Kod:

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class TenantSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 10;

    public TenantSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var exists = await _db.Tenants
            .AnyAsync(x => x.Code == "DEMO", cancellationToken);

        if (exists)
            return;

        var tenant = new Tenant
        {
            Code = "DEMO",
            Name = "Demo Tenant",
            IsActive = true
        };

        _db.Tenants.Add(tenant);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

To jest pierwszy przykład idempotencji.

Najpierw sprawdzamy, czy tenant `DEMO` już istnieje.

Jeśli istnieje, nic nie robimy.

Jeśli nie istnieje, tworzymy go.

Dzięki temu seed można uruchomić wiele razy.

---

# Krok 5 — OrganizationUnitSeedStep

Teraz dodajemy jednostki organizacyjne.

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/OrganizationUnitSeedStep.cs
```

Kod:

```csharp
using HelpDeskHero.Domain.Entities;
using HelpDeskHero.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class OrganizationUnitSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 20;

    public OrganizationUnitSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var exists = await _db.OrganizationUnits
            .AnyAsync(x => x.TenantId == tenant.Id && x.Code == "IT", cancellationToken);

        if (exists)
            return;

        var it = new OrganizationUnit
        {
            TenantId = tenant.Id,
            Code = "IT",
            Name = "IT Department",
            Type = OrganizationUnitType.Department,
            IsActive = true
        };

        var helpdesk = new OrganizationUnit
        {
            TenantId = tenant.Id,
            ParentOrganizationUnit = it,
            Code = "HELPDESK",
            Name = "Helpdesk Team",
            Type = OrganizationUnitType.Team,
            IsActive = true
        };

        var infrastructure = new OrganizationUnit
        {
            TenantId = tenant.Id,
            ParentOrganizationUnit = it,
            Code = "INFRA",
            Name = "Infrastructure Team",
            Type = OrganizationUnitType.Team,
            IsActive = true
        };

        _db.OrganizationUnits.AddRange(it, helpdesk, infrastructure);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

Tutaj tworzymy prostą strukturę:

```text
IT Department
  -> Helpdesk Team
  -> Infrastructure Team
```

To pozwoli później przypisywać tickety do jednostek organizacyjnych.

---

# Krok 6 — TicketTypeSeedStep

Teraz typy ticketów.

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/TicketTypeSeedStep.cs
```

Kod:

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class TicketTypeSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 30;

    public TicketTypeSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var existingCodes = await _db.TicketTypes
            .Where(x => x.TenantId == tenant.Id)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var items = new List<TicketType>();

        if (!existingCodes.Contains("INCIDENT"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "INCIDENT",
                Name = "Incident",
                Description = "Unexpected interruption or degradation of service.",
                IsActive = true
            });
        }

        if (!existingCodes.Contains("SERVICE_REQUEST"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "SERVICE_REQUEST",
                Name = "Service Request",
                Description = "Standard user request.",
                IsActive = true
            });
        }

        if (!existingCodes.Contains("ACCESS_REQUEST"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "ACCESS_REQUEST",
                Name = "Access Request",
                Description = "Request for access to system or resource.",
                IsActive = true
            });
        }

        if (items.Count == 0)
            return;

        _db.TicketTypes.AddRange(items);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

Dodajemy trzy typy:

```text
Incident
Service Request
Access Request
```

To wystarczy na pierwsze demo.

W kolejnych odcinkach możemy pokazać, że każdy typ może mieć własny workflow.

---

# Krok 7 — WorkflowSeedStep

Teraz najważniejszy seed tego odcinka: workflow.

Tworzymy plik:

```text
src/HelpDeskHero.Infrastructure/Persistence/Seeding/WorkflowSeedStep.cs
```

Kod:

```csharp
using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class WorkflowSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 40;

    public WorkflowSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var incidentType = await _db.TicketTypes
            .SingleAsync(x => x.TenantId == tenant.Id && x.Code == "INCIDENT", cancellationToken);

        var workflowExists = await _db.WorkflowDefinitions
            .AnyAsync(x =>
                x.TenantId == tenant.Id &&
                x.TicketTypeId == incidentType.Id &&
                x.Code == "INCIDENT_DEFAULT",
                cancellationToken);

        if (workflowExists)
            return;

        var workflow = new WorkflowDefinition
        {
            TenantId = tenant.Id,
            TicketTypeId = incidentType.Id,
            Code = "INCIDENT_DEFAULT",
            Name = "Incident Default Workflow",
            IsActive = true,
            IsDefault = true
        };

        var newState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "NEW",
            Name = "New",
            IsStart = true,
            IsFinal = false,
            SortOrder = 10
        };

        var triagedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "TRIAGED",
            Name = "Triaged",
            IsStart = false,
            IsFinal = false,
            SortOrder = 20
        };

        var inProgressState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "IN_PROGRESS",
            Name = "In Progress",
            IsStart = false,
            IsFinal = false,
            SortOrder = 30
        };

        var resolvedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "RESOLVED",
            Name = "Resolved",
            IsStart = false,
            IsFinal = false,
            SortOrder = 40
        };

        var closedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "CLOSED",
            Name = "Closed",
            IsStart = false,
            IsFinal = true,
            SortOrder = 50
        };

        workflow.States.Add(newState);
        workflow.States.Add(triagedState);
        workflow.States.Add(inProgressState);
        workflow.States.Add(resolvedState);
        workflow.States.Add(closedState);

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = newState,
            ToState = triagedState,
            Code = "NEW_TO_TRIAGED",
            Name = "New -> Triaged",
            RequiresComment = false,
            IsActive = true
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = triagedState,
            ToState = inProgressState,
            Code = "TRIAGED_TO_IN_PROGRESS",
            Name = "Triaged -> In Progress",
            RequiresComment = false,
            IsActive = true
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = inProgressState,
            ToState = resolvedState,
            Code = "IN_PROGRESS_TO_RESOLVED",
            Name = "In Progress -> Resolved",
            RequiresComment = true,
            IsActive = true
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = resolvedState,
            ToState = closedState,
            Code = "RESOLVED_TO_CLOSED",
            Name = "Resolved -> Closed",
            RequiresComment = false,
            IsActive = true
        });

        _db.WorkflowDefinitions.Add(workflow);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

Ten krok tworzy domyślny workflow dla typu `INCIDENT`.

Mamy stany:

```text
NEW
TRIAGED
IN_PROGRESS
RESOLVED
CLOSED
```

I przejścia:

```text
NEW -> TRIAGED
TRIAGED -> IN_PROGRESS
IN_PROGRESS -> RESOLVED
RESOLVED -> CLOSED
```

To jest pierwszy realny proces w naszym systemie.

---

## Dlaczego workflow seedujemy?

Bo bez workflow nie możemy sensownie utworzyć ticketu.

Ticket potrzebuje stanu początkowego.

A stan początkowy wynika z aktywnego workflow dla danego typu ticketu.

To pokazuje, że seed danych nie jest tylko dodatkiem.

W aplikacji procesowej część konfiguracji startowej jest warunkiem działania systemu.

---

# Krok 8 — rejestracja seeda w DI

Teraz rejestrujemy seedery.

W pliku:

```text
src/HelpDeskHero.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
```

dodajemy:

```csharp
using HelpDeskHero.Infrastructure.Persistence.Seeding;
```

oraz w metodzie `AddInfrastructure`:

```csharp
services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

services.AddScoped<ISeedStep, TenantSeedStep>();
services.AddScoped<ISeedStep, OrganizationUnitSeedStep>();
services.AddScoped<ISeedStep, TicketTypeSeedStep>();
services.AddScoped<ISeedStep, WorkflowSeedStep>();
```

Czyli pełny fragment może wyglądać tak:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

services.AddScoped<ISeedStep, TenantSeedStep>();
services.AddScoped<ISeedStep, OrganizationUnitSeedStep>();
services.AddScoped<ISeedStep, TicketTypeSeedStep>();
services.AddScoped<ISeedStep, WorkflowSeedStep>();
```

---

# Krok 9 — jak uruchomić seed?

W development można uruchomić seed przy starcie aplikacji, ale trzeba to zrobić świadomie.

Można przygotować metodę rozszerzającą:

```text
src/HelpDeskHero.Api/Extensions/DatabaseExtensions.cs
```

Kod:

```csharp
using HelpDeskHero.Infrastructure.Persistence.Seeding;

namespace HelpDeskHero.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        await seeder.SeedAsync(CancellationToken.None);
    }
}
```

W `Program.cs` dla development:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.SeedDatabaseAsync();
}
```

Ale trzeba tutaj bardzo jasno powiedzieć:

```text
To jest wygodne w development.
W production seed powinien być częścią kontrolowanego procesu migracyjnego albo narzędzia administracyjnego.
```

---

# Krok 10 — produkcyjny sposób uruchamiania seeda

W produkcji seed powinien być uruchamiany przez proces wdrożeniowy.

Na przykład:

```text
1. DBA tworzy bazę i konta.
2. Migrator wykonuje migracje.
3. Migrator uruchamia seed danych.
4. Aplikacja runtime startuje na ograniczonym koncie.
```

Można też przygotować osobną komendę albo osobny projekt CLI:

```text
HelpDeskHero.AdminTools
```

Na razie tego nie budujemy, ale warto powiedzieć, że docelowo seed nie musi być wykonywany przez API przy starcie.

---

# Krok 11 — sprawdzenie danych w SQL Server

Po uruchomieniu seeda można sprawdzić dane:

```sql
SELECT * FROM dbo.Tenants;
SELECT * FROM dbo.OrganizationUnits;
SELECT * FROM dbo.TicketTypes;
SELECT * FROM dbo.WorkflowDefinitions;
SELECT * FROM dbo.WorkflowStates;
SELECT * FROM dbo.WorkflowTransitions;
```

Warto pokazać widzowi, że:

```text
tenant istnieje,
jednostki organizacyjne istnieją,
typy ticketów istnieją,
workflow istnieje,
stany istnieją,
przejścia istnieją.
```

To jest świetny moment, żeby wrócić do decyzji z odcinka 2:

```text
status ticketu wynika z workflow.
```

Teraz widzimy ten workflow fizycznie w bazie.

---

# Krok 12 — idempotencja

Najważniejsza właściwość seeda to idempotencja.

Seed powinien dać się uruchomić wiele razy.

Nie powinien tworzyć duplikatów.

Dlatego każdy seed step sprawdza, czy dane już istnieją.

Przykład:

```csharp
var exists = await _db.Tenants
    .AnyAsync(x => x.Code == "DEMO", cancellationToken);

if (exists)
    return;
```

Albo dla typów ticketów sprawdzamy istniejące kody.

To bardzo ważne.

Bo w praktyce seed może zostać uruchomiony wielokrotnie:

w development,  
po migracji,  
na środowisku testowym,  
w pipeline,  
w środowisku demo.

---

# Krok 13 — czego nie seedujemy w tym odcinku?

Na razie nie seedujemy:

```text
użytkowników Identity,
ról,
permissions,
ticketów demo,
komentarzy,
załączników,
SLA,
webhooków,
KPI.
```

Dlaczego?

Bo ten odcinek ma dotyczyć podstawowej konfiguracji domenowej.

Identity zrobimy osobno.

SLA i powiadomienia zrobimy później.

A tickety demonstracyjne dodamy dopiero wtedy, gdy będziemy mieć application services albo API.

---

# Ważna decyzja: seed domenowy vs seed demonstracyjny

Warto rozróżnić dwie rzeczy.

Pierwsza rzecz to seed domenowy albo konfiguracyjny.

To są dane potrzebne do działania aplikacji:

```text
tenant,
typy ticketów,
workflow,
stany,
przejścia.
```

Druga rzecz to seed demonstracyjny.

To są przykładowe tickety, komentarze i dane testowe.

Dane domenowe mogą być potrzebne w produkcji.

Dane demonstracyjne raczej nie.

Dlatego warto trzymać je osobno.

Na przykład:

```text
WorkflowSeedStep
DemoTicketSeedStep
```

i uruchamiać demo seed tylko w development albo demo environment.

---

# Podsumowanie

Podsumowując.

W tym odcinku przygotowaliśmy seed danych w uporządkowany sposób.

Nie stworzyliśmy jednego wielkiego `DbInitializer`.

Zamiast tego mamy:

```text
ISeedStep
IDatabaseSeeder
DatabaseSeeder
TenantSeedStep
OrganizationUnitSeedStep
TicketTypeSeedStep
WorkflowSeedStep
```

Dodaliśmy pierwsze dane:

```text
Demo Tenant
IT Department
Helpdesk Team
Infrastructure Team
Incident
Service Request
Access Request
Incident Default Workflow
stany workflow
przejścia workflow
```

Najważniejsza lekcja:

```text
Seed danych powinien być idempotentny i podzielony na odpowiedzialności.
```

W kolejnym odcinku możemy przejść do pierwszego application service dla ticketów albo do Identity i ról.

Moim zdaniem naturalny kolejny krok to application service do tworzenia ticketu, bo mamy już domenę, EF Core, migracje i seed workflow.

Dzięki za uwagę i do zobaczenia w kolejnym materiale.

---

# Krótsze zakończenie

Na dziś tyle.

Mamy seed danych w osobnych klasach.

Mamy tenanta, jednostki organizacyjne, typy ticketów i pierwszy workflow.

To oznacza, że przygotowaliśmy konfigurację potrzebną do utworzenia pierwszego ticketu.

W kolejnym odcinku możemy przejść do pierwszego przypadku użycia: tworzenia ticketu.

Dzięki za uwagę.

---

# Notatki dla prowadzącego

## Co pokazać na ekranie

- katalog `Persistence/Seeding`,
- `ISeedStep`,
- `DatabaseSeeder`,
- `TenantSeedStep`,
- `OrganizationUnitSeedStep`,
- `TicketTypeSeedStep`,
- `WorkflowSeedStep`,
- rejestrację w DI,
- opcjonalne uruchomienie seeda w development,
- zapytania SQL sprawdzające dane,
- workflow w tabelach.

## Najważniejsze zdania

1. Seed danych to nie śmietnik na wszystko.
2. Jeden wielki `DbInitializer` szybko robi się problemem.
3. Jeden obszar powinien mieć jeden seed step.
4. Seed powinien być idempotentny.
5. Dane konfiguracyjne i dane demonstracyjne to nie to samo.
6. Workflow musi istnieć, żeby utworzyć sensowny ticket.
7. W production seed powinien być częścią kontrolowanego procesu migracyjnego.

## Możliwe CTA

Jeśli interesuje Cię EF Core w praktyce, w kolejnym odcinku możemy przejść do pierwszego przypadku użycia: utworzenia ticketu z wykorzystaniem application service, walidacji i danych startowych z workflow.
