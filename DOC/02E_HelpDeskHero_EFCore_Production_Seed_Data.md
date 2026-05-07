# HelpDeskHero - EF Core Production Seed Data

> Cel rozdziału: pokazać bezpieczny, produkcyjny mechanizm seedowania danych startowych w EF Core, w którym każda grupa danych ma osobną klasę, a uruchomienie seeda odbywa się kontrolowanie przez konto migracyjne, a nie przez konto aplikacji runtime.

---

## 1. Dlaczego nie jeden wielki DbInitializer

W prostych demach często spotyka się jedną klasę typu `DbInitializer`, w której tworzymy wszystko naraz:

- tenantów,
- role,
- użytkowników,
- typy ticketów,
- workflow,
- SLA,
- artykuły knowledge base,
- testowe zgłoszenia.

W produkcyjnym projekcie to szybko staje się trudne do utrzymania. Dlatego w HelpDeskHero przyjmujemy zasadę:

```text
Każdy obszar danych startowych ma własną klasę seeda.
```

Dzięki temu łatwiej pokazać mechanizm na szkoleniu i łatwiej rozwijać projekt bez chaosu.

---

## 2. Najważniejsza zasada bezpieczeństwa

Seed danych produkcyjnych nie powinien być wykonywany przez zwykłe konto aplikacji.

Przyjmujemy trzy poziomy odpowiedzialności:

```text
DBA / administrator SQL Server
    tworzy bazę, loginy, userów i role techniczne

hdh_migrator
    wykonuje migracje EF Core i kontrolowany seed danych startowych

hdh_app
    działa w runtime aplikacji i ma minimalne uprawnienia DML
```

Czyli:

```text
hdh_app nie robi Database.Migrate()
hdh_app nie zakłada schematów
hdh_app nie tworzy ról technicznych
hdh_app nie wykonuje produkcyjnego seeda
```

Seed uruchamiany jest jako osobne polecenie administracyjne, pipeline deployment albo krok po migracji z connection stringiem migracyjnym.

---

## 3. Gdzie trzymać seedery

Proponowana struktura katalogów:

```text
src
└── HelpDeskHero.Api
    └── Infrastructure
        └── Persistence
            └── Seeding
                ├── IDatabaseSeeder.cs
                ├── ISeedStep.cs
                ├── DatabaseSeeder.cs
                ├── SeedDataOptions.cs
                ├── IdentitySeedStep.cs
                ├── TenantSeedStep.cs
                ├── OrganizationUnitSeedStep.cs
                ├── TicketTypeSeedStep.cs
                ├── WorkflowSeedStep.cs
                ├── TicketSlaPolicySeedStep.cs
                ├── KnowledgeBaseSeedStep.cs
                ├── WebhookSubscriptionSeedStep.cs
                └── KpiSeedStep.cs
```

Ten układ jest czytelny:

- `DatabaseSeeder` orkiestruje wykonanie,
- `ISeedStep` definiuje pojedynczy krok,
- każda encja / obszar ma własny seeder.

---

## 4. Interfejs pojedynczego kroku seeda

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/ISeedStep.cs
```

```csharp
namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public interface ISeedStep
{
    int Order { get; }
    string Name { get; }
    Task SeedAsync(CancellationToken ct = default);
}
```

`Order` pozwala wymusić kolejność. To jest ważne, bo np. `WorkflowDefinition` zależy od `Tenant` i `TicketType`.

---

## 5. Interfejs głównego seeda

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/IDatabaseSeeder.cs
```

```csharp
namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
```

---

## 6. Orkiestrator seeda

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs
```

```csharp
using Microsoft.Extensions.Logging;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

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

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var orderedSteps = _steps
            .OrderBy(x => x.Order)
            .ToList();

        _logger.LogInformation("Starting database seed. Steps: {Count}", orderedSteps.Count);

        foreach (var step in orderedSteps)
        {
            _logger.LogInformation("Running seed step {Order}: {Name}", step.Order, step.Name);
            await step.SeedAsync(ct);
            _logger.LogInformation("Completed seed step {Order}: {Name}", step.Order, step.Name);
        }

        _logger.LogInformation("Database seed completed.");
    }
}
```

---

## 7. Opcje seeda

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/SeedDataOptions.cs
```

```csharp
namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class SeedDataOptions
{
    public bool Enabled { get; set; }
    public string DefaultTenantCode { get; set; } = "DEMO";
    public string DefaultTenantName { get; set; } = "Demo Company";
    public string AdminEmail { get; set; } = "admin@helpdeskhero.local";
    public string AdminPassword { get; set; } = "ChangeMe-Admin123!";
    public bool CreateDemoTickets { get; set; }
    public bool CreateDemoKnowledgeBase { get; set; } = true;
}
```

Przykład konfiguracji:

```json
{
  "SeedData": {
    "Enabled": true,
    "DefaultTenantCode": "DEMO",
    "DefaultTenantName": "Demo Company",
    "AdminEmail": "admin@helpdeskhero.local",
    "AdminPassword": "ChangeMe-Admin123!",
    "CreateDemoTickets": false,
    "CreateDemoKnowledgeBase": true
  }
}
```

W produkcji hasło admina powinno pochodzić z bezpiecznego źródła, np. secret managera, zmiennej środowiskowej albo pipeline secret.

---

## 8. IdentitySeedStep - role i administrator

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/IdentitySeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class IdentitySeedStep : ISeedStep
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedDataOptions _options;

    public IdentitySeedStep(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedDataOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public int Order => 10;
    public string Name => "Identity roles and default administrator";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        string[] roles = ["Admin", "Manager", "Agent", "User"];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Cannot create role {role}: {FormatErrors(result)}");
            }
        }

        var admin = await _userManager.FindByEmailAsync(_options.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = _options.AdminEmail,
                Email = _options.AdminEmail,
                EmailConfirmed = true,
                DisplayName = "System Administrator",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(admin, _options.AdminPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Cannot create admin user: {FormatErrors(createResult)}");
        }

        if (!await _userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await _userManager.AddToRoleAsync(admin, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Cannot assign Admin role: {FormatErrors(roleResult)}");
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));
}
```

---

## 9. TenantSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/TenantSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class TenantSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public TenantSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 20;
    public string Name => "Default tenant";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var exists = await _db.Tenants
            .AnyAsync(x => x.Code == _options.DefaultTenantCode, ct);

        if (exists)
            return;

        _db.Tenants.Add(new Tenant
        {
            Id = Guid.NewGuid(),
            Code = _options.DefaultTenantCode,
            Name = _options.DefaultTenantName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
```

Seed jest idempotentny: jeśli tenant istnieje, krok niczego nie duplikuje.

---

## 10. OrganizationUnitSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/OrganizationUnitSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class OrganizationUnitSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public OrganizationUnitSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 30;
    public string Name => "Default organization units";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var units = new[]
        {
            new { Name = "IT", Type = "Department" },
            new { Name = "HR", Type = "Department" },
            new { Name = "Finance", Type = "Department" }
        };

        foreach (var unit in units)
        {
            var exists = await _db.OrganizationUnits
                .AnyAsync(x => x.TenantId == tenant.Id && x.Name == unit.Name, ct);

            if (exists)
                continue;

            _db.OrganizationUnits.Add(new OrganizationUnit
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = unit.Name,
                Type = unit.Type
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

---

## 11. TicketTypeSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/TicketTypeSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class TicketTypeSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public TicketTypeSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 40;
    public string Name => "Default ticket types";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var types = new[]
        {
            new { Code = "INCIDENT", Name = "Incident" },
            new { Code = "SERVICE_REQUEST", Name = "Service Request" },
            new { Code = "ACCESS_REQUEST", Name = "Access Request" }
        };

        foreach (var type in types)
        {
            var exists = await _db.TicketTypes
                .AnyAsync(x => x.TenantId == tenant.Id && x.Code == type.Code, ct);

            if (exists)
                continue;

            _db.TicketTypes.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = type.Code,
                Name = type.Name,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

---

## 12. WorkflowSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/WorkflowSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class WorkflowSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public WorkflowSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 50;
    public string Name => "Default workflows";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        await SeedIncidentWorkflowAsync(tenant.Id, ct);
        await SeedAccessRequestWorkflowAsync(tenant.Id, ct);
    }

    private async Task SeedIncidentWorkflowAsync(Guid tenantId, CancellationToken ct)
    {
        var ticketType = await _db.TicketTypes
            .SingleAsync(x => x.TenantId == tenantId && x.Code == "INCIDENT", ct);

        var exists = await _db.WorkflowDefinitions
            .AnyAsync(x => x.TenantId == tenantId && x.TicketTypeId == ticketType.Id && x.Name == "Default Incident Workflow", ct);

        if (exists)
            return;

        var workflow = new WorkflowDefinition
        {
            TenantId = tenantId,
            TicketTypeId = ticketType.Id,
            Name = "Default Incident Workflow",
            IsDefault = true
        };

        var states = new List<WorkflowState>
        {
            new() { WorkflowDefinition = workflow, Code = "NEW", Name = "New", IsStart = true, IsEnd = false, SortOrder = 10 },
            new() { WorkflowDefinition = workflow, Code = "TRIAGED", Name = "Triaged", IsStart = false, IsEnd = false, SortOrder = 20 },
            new() { WorkflowDefinition = workflow, Code = "IN_PROGRESS", Name = "In Progress", IsStart = false, IsEnd = false, SortOrder = 30 },
            new() { WorkflowDefinition = workflow, Code = "RESOLVED", Name = "Resolved", IsStart = false, IsEnd = false, SortOrder = 40 },
            new() { WorkflowDefinition = workflow, Code = "CLOSED", Name = "Closed", IsStart = false, IsEnd = true, SortOrder = 50 }
        };

        workflow.States = states;

        _db.WorkflowDefinitions.Add(workflow);
        await _db.SaveChangesAsync(ct);

        await AddTransitionAsync(workflow.Id, "NEW", "TRIAGED", "Triage", "Agent", ct);
        await AddTransitionAsync(workflow.Id, "TRIAGED", "IN_PROGRESS", "Start work", "Agent", ct);
        await AddTransitionAsync(workflow.Id, "IN_PROGRESS", "RESOLVED", "Resolve", "Agent", ct);
        await AddTransitionAsync(workflow.Id, "RESOLVED", "CLOSED", "Close", "Manager", ct);
    }

    private async Task SeedAccessRequestWorkflowAsync(Guid tenantId, CancellationToken ct)
    {
        var ticketType = await _db.TicketTypes
            .SingleAsync(x => x.TenantId == tenantId && x.Code == "ACCESS_REQUEST", ct);

        var exists = await _db.WorkflowDefinitions
            .AnyAsync(x => x.TenantId == tenantId && x.TicketTypeId == ticketType.Id && x.Name == "Default Access Request Workflow", ct);

        if (exists)
            return;

        var workflow = new WorkflowDefinition
        {
            TenantId = tenantId,
            TicketTypeId = ticketType.Id,
            Name = "Default Access Request Workflow",
            IsDefault = true
        };

        var states = new List<WorkflowState>
        {
            new() { WorkflowDefinition = workflow, Code = "NEW", Name = "New", IsStart = true, IsEnd = false, SortOrder = 10 },
            new() { WorkflowDefinition = workflow, Code = "MANAGER_APPROVAL", Name = "Manager Approval", IsStart = false, IsEnd = false, SortOrder = 20 },
            new() { WorkflowDefinition = workflow, Code = "SECURITY_APPROVAL", Name = "Security Approval", IsStart = false, IsEnd = false, SortOrder = 30 },
            new() { WorkflowDefinition = workflow, Code = "COMPLETED", Name = "Completed", IsStart = false, IsEnd = true, SortOrder = 40 }
        };

        workflow.States = states;

        _db.WorkflowDefinitions.Add(workflow);
        await _db.SaveChangesAsync(ct);

        await AddTransitionAsync(workflow.Id, "NEW", "MANAGER_APPROVAL", "Send to manager", "User", ct);
        await AddTransitionAsync(workflow.Id, "MANAGER_APPROVAL", "SECURITY_APPROVAL", "Manager approved", "Manager", ct);
        await AddTransitionAsync(workflow.Id, "SECURITY_APPROVAL", "COMPLETED", "Security approved", "Admin", ct);
    }

    private async Task AddTransitionAsync(
        int workflowDefinitionId,
        string fromCode,
        string toCode,
        string name,
        string requiredRole,
        CancellationToken ct)
    {
        var from = await _db.WorkflowStates
            .SingleAsync(x => x.WorkflowDefinitionId == workflowDefinitionId && x.Code == fromCode, ct);

        var to = await _db.WorkflowStates
            .SingleAsync(x => x.WorkflowDefinitionId == workflowDefinitionId && x.Code == toCode, ct);

        _db.WorkflowTransitions.Add(new WorkflowTransition
        {
            WorkflowDefinitionId = workflowDefinitionId,
            FromStateId = from.Id,
            ToStateId = to.Id,
            Name = name,
            RequiredRole = requiredRole
        });

        await _db.SaveChangesAsync(ct);
    }
}
```

Uwaga: w konfiguracji encji warto dodać unikalne indeksy, które zabezpieczą przed duplikatami workflow i stanów.

---

## 13. TicketSlaPolicySeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/TicketSlaPolicySeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class TicketSlaPolicySeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public TicketSlaPolicySeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 60;
    public string Name => "Default SLA policies";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var policies = new[]
        {
            new { Name = "High priority SLA", Priority = "High", FirstResponse = 30, Resolve = 240 },
            new { Name = "Medium priority SLA", Priority = "Medium", FirstResponse = 120, Resolve = 1440 },
            new { Name = "Low priority SLA", Priority = "Low", FirstResponse = 480, Resolve = 2880 }
        };

        foreach (var policy in policies)
        {
            var exists = await _db.TicketSlaPolicies
                .AnyAsync(x => x.TenantId == tenant.Id && x.Priority == policy.Priority, ct);

            if (exists)
                continue;

            _db.TicketSlaPolicies.Add(new TicketSlaPolicy
            {
                TenantId = tenant.Id,
                Name = policy.Name,
                Priority = policy.Priority,
                FirstResponseMinutes = policy.FirstResponse,
                ResolveMinutes = policy.Resolve,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

Jeśli w obecnej encji `TicketSlaPolicy` nie ma jeszcze `TenantId`, dodaj go. W modelu produkcyjnym polityki SLA powinny być izolowane per tenant.

---

## 14. KnowledgeBaseSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/KnowledgeBaseSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class KnowledgeBaseSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public KnowledgeBaseSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 70;
    public string Name => "Default knowledge base articles";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_options.CreateDemoKnowledgeBase)
            return;

        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var articles = new[]
        {
            new
            {
                Slug = "vpn-basic-troubleshooting",
                Title = "VPN - basic troubleshooting",
                Summary = "First steps when VPN connection fails.",
                Category = "Network",
                Content = "# VPN - basic troubleshooting\n\n1. Check internet connection.\n2. Restart VPN client.\n3. Verify MFA prompt."
            },
            new
            {
                Slug = "password-reset-guide",
                Title = "Password reset guide",
                Summary = "How to reset user password safely.",
                Category = "Identity",
                Content = "# Password reset guide\n\nUse approved identity process and verify the user before reset."
            }
        };

        foreach (var article in articles)
        {
            var exists = await _db.KnowledgeArticles
                .AnyAsync(x => x.TenantId == tenant.Id && x.Slug == article.Slug, ct);

            if (exists)
                continue;

            _db.KnowledgeArticles.Add(new KnowledgeArticle
            {
                TenantId = tenant.Id,
                Slug = article.Slug,
                Title = article.Title,
                Summary = article.Summary,
                Category = article.Category,
                ContentMarkdown = article.Content,
                IsPublished = true,
                CreatedAtUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

---

## 15. WebhookSubscriptionSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/WebhookSubscriptionSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class WebhookSubscriptionSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public WebhookSubscriptionSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 80;
    public string Name => "Default webhook subscriptions";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var eventName = "ticket.created";
        var targetUrl = "https://example.invalid/helpdeskhero/webhook";

        var exists = await _db.WebhookSubscriptions
            .AnyAsync(x => x.TenantId == tenant.Id && x.EventName == eventName && x.TargetUrl == targetUrl, ct);

        if (exists)
            return;

        _db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            TenantId = tenant.Id,
            EventName = eventName,
            TargetUrl = targetUrl,
            Secret = "CHANGE-ME-WEBHOOK-SECRET",
            IsActive = false
        });

        await _db.SaveChangesAsync(ct);
    }
}
```

Domyślnie webhook jest `IsActive = false`, bo przykładowy adres nie powinien być aktywny produkcyjnie.

---

## 16. KpiSeedStep

Plik:

```text
src/HelpDeskHero.Api/Infrastructure/Persistence/Seeding/KpiSeedStep.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

public sealed class KpiSeedStep : ISeedStep
{
    private readonly AppDbContext _db;
    private readonly SeedDataOptions _options;

    public KpiSeedStep(AppDbContext db, IOptions<SeedDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public int Order => 90;
    public string Name => "Initial KPI snapshot";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == _options.DefaultTenantCode, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var exists = await _db.KpiSnapshots
            .AnyAsync(x => x.TenantId == tenant.Id && x.SnapshotDate == today, ct);

        if (exists)
            return;

        _db.KpiSnapshots.Add(new KpiSnapshot
        {
            TenantId = tenant.Id,
            SnapshotDate = today,
            OpenTickets = 0,
            ClosedTickets = 0,
            BreachedSlaTickets = 0,
            AverageResolutionMinutes = 0,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
```

Jeśli `KpiSnapshot` ma inną strukturę pól, dostosuj nazwy, ale sama zasada pozostaje taka sama: osobny krok seeda, idempotentny, per tenant.

---

## 17. Rejestracja seederów w DI

Plik:

```text
src/HelpDeskHero.Api/Program.cs
```

```csharp
using HelpDeskHero.Api.Infrastructure.Persistence.Seeding;

builder.Services.Configure<SeedDataOptions>(
    builder.Configuration.GetSection("SeedData"));

builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

builder.Services.AddScoped<ISeedStep, IdentitySeedStep>();
builder.Services.AddScoped<ISeedStep, TenantSeedStep>();
builder.Services.AddScoped<ISeedStep, OrganizationUnitSeedStep>();
builder.Services.AddScoped<ISeedStep, TicketTypeSeedStep>();
builder.Services.AddScoped<ISeedStep, WorkflowSeedStep>();
builder.Services.AddScoped<ISeedStep, TicketSlaPolicySeedStep>();
builder.Services.AddScoped<ISeedStep, KnowledgeBaseSeedStep>();
builder.Services.AddScoped<ISeedStep, WebhookSubscriptionSeedStep>();
builder.Services.AddScoped<ISeedStep, KpiSeedStep>();
```

Rejestracja wielu implementacji `ISeedStep` pozwala `DatabaseSeeder` pobrać je jako `IEnumerable<ISeedStep>`.

---

## 18. Jak uruchamiać seed bezpiecznie

### Wariant rekomendowany: osobny tryb administracyjny

W produkcji nie uruchamiaj seeda automatycznie przy każdym starcie API.

Zamiast tego możesz dodać tryb uruchomienia aplikacji:

```powershell
dotnet run --project .\src\HelpDeskHero.Api -- --seed
```

W `Program.cs`:

```csharp
var app = builder.Build();

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
    return;
}
```

Ten tryb powinien być uruchamiany z connection stringiem migracyjnym, np. `MigrationConnection`.

---

## 19. Connection string dla seeda

W produkcji rozdziel:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL01;Database=HelpDeskHeroDb;User Id=hdh_app;Password=...;TrustServerCertificate=True;",
    "MigrationConnection": "Server=SQL01;Database=HelpDeskHeroDb;User Id=hdh_migrator;Password=...;TrustServerCertificate=True;"
  }
}
```

Dla trybu `--seed` można wymusić użycie `MigrationConnection`.

Przykładowy wybór connection stringa:

```csharp
var isMigrationOrSeedMode = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)
    || args.Contains("--seed", StringComparer.OrdinalIgnoreCase);

var connectionStringName = isMigrationOrSeedMode
    ? "MigrationConnection"
    : "DefaultConnection";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(connectionStringName)));
```

---

## 20. Dlaczego nie HasData dla całego seeda

`HasData` w EF Core jest dobre dla bardzo prostych, stabilnych danych słownikowych.

W HelpDeskHero lepiej nie opierać całego seeda o `HasData`, bo mamy:

- Identity,
- hasła i hashe,
- workflow ze stanami i przejściami,
- tenanty,
- SLA per tenant,
- knowledge base,
- webhooki,
- dane zależne od konfiguracji.

Dlatego preferujemy seedy imperative, czyli klasy wykonujące logiczne kroki.

Dobre użycie `HasData`:

```text
małe, techniczne słowniki, które prawie nigdy się nie zmieniają
```

Lepsze użycie klas seeda:

```text
tenanty, role, użytkownicy, workflow, SLA, KB, webhooks, dane demonstracyjne
```

---

## 21. Idempotencja seeda

Każdy seed step musi być idempotentny.

To znaczy, że ponowne uruchomienie:

```powershell
dotnet run --project .\src\HelpDeskHero.Api -- --seed
```

nie powinno tworzyć duplikatów.

Przykład kontroli:

```csharp
var exists = await _db.TicketTypes
    .AnyAsync(x => x.TenantId == tenant.Id && x.Code == "INCIDENT", ct);

if (exists)
    return;
```

Dodatkowo warto zabezpieczyć to indeksami unikalnymi w konfiguracjach EF Core.

---

## 22. Indeksy unikalne wspierające seed

W konfiguracjach encji warto mieć między innymi:

```csharp
builder.HasIndex(x => x.Code).IsUnique();
```

Dla multi-tenant zwykle:

```csharp
builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
```

Przykłady:

```text
Tenant.Code
OrganizationUnit: TenantId + Name
TicketType: TenantId + Code
WorkflowDefinition: TenantId + TicketTypeId + Name
WorkflowState: WorkflowDefinitionId + Code
KnowledgeArticle: TenantId + Slug
WebhookSubscription: TenantId + EventName + TargetUrl
KpiSnapshot: TenantId + SnapshotDate
```

Dzięki temu nawet błąd w seedzie nie powinien cicho zdublować danych.

---

## 23. Checklist wdrożeniowy

Przed nagraniem lub wdrożeniem sprawdź:

```text
[ ] Każdy obszar seeda ma osobną klasę ISeedStep.
[ ] DatabaseSeeder uruchamia kroki w ustalonej kolejności.
[ ] Seed jest idempotentny.
[ ] Seed nie działa automatycznie przy starcie runtime API.
[ ] Seed używa konta migracyjnego, nie konta aplikacyjnego.
[ ] Hasła i sekrety nie są zapisane w repozytorium.
[ ] Encje mają indeksy unikalne chroniące przed duplikatami.
[ ] Tenant, workflow, SLA i KB są seedowane osobno.
[ ] hdh_app nie ma uprawnień DDL.
[ ] hdh_migrator ma uprawnienia potrzebne do migracji i seeda.
```

---

## 24. Przekaz do nagrania

Najważniejsza myśl:

```text
Seed danych to nie śmietnik na kod startowy.
To kontrolowany mechanizm inicjalizacji systemu.
```

W HelpDeskHero pokazujemy wzorzec:

```text
osobne seedery
idempotencja
kolejność wykonania
minimalne uprawnienia runtime
konto migracyjne do migracji i seeda
```

To dobrze pokazuje, że EF Core może być używany produkcyjnie i bezpiecznie, bez dawania aplikacji roli `db_owner`.
