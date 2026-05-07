# 02 - Domena, EF Core i SQL Server

> Wersja: **HelpDeskHero Production Edition**  
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.

Ten plik opisuje **docelowy produkcyjny model domenowy**. W tej wersji `Ticket` nie jest jedyną encją. Jest centralnym agregatem biznesowym, ale działa w kontekście tenantów, jednostek organizacyjnych, typów zgłoszeń, workflow, bazy wiedzy, webhooków, KPI, audytu, SLA i outboxa.

---

## 1. Pełny model domenowy

```text
Tenant
OrganizationUnit
ApplicationUser
RefreshToken
TicketType
WorkflowDefinition
WorkflowState
WorkflowTransition
Ticket
TicketComment
TicketAttachment
TicketSlaPolicy
TicketEscalation
KnowledgeArticle
WebhookSubscription
KpiSnapshot
AuditLog
UserNotification
OutboxMessage
```

Relacje logiczne:

```text
Tenant
 ├── OrganizationUnit
 ├── ApplicationUser
 ├── TicketType
 │    └── WorkflowDefinition
 │         ├── WorkflowState
 │         └── WorkflowTransition
 ├── KnowledgeArticle
 ├── WebhookSubscription
 ├── KpiSnapshot
 └── Ticket
      ├── TicketComment
      ├── TicketAttachment
      ├── TicketEscalation
      ├── AuditLog
      └── OutboxMessage
```

Zasada bazowa: każda encja biznesowa, która ma być izolowana między tenantami, ma `TenantId`.

---

## 2. Encje domenowe

### Tenant

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<OrganizationUnit> OrganizationUnits { get; set; } = [];
    public List<TicketType> TicketTypes { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
    public List<KnowledgeArticle> KnowledgeArticles { get; set; } = [];
    public List<WebhookSubscription> WebhookSubscriptions { get; set; } = [];
}
```

### OrganizationUnit

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class OrganizationUnit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ParentUnitId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Department";
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = default!;
    public OrganizationUnit? ParentUnit { get; set; }
    public List<OrganizationUnit> Children { get; set; } = [];
}
```

### ApplicationUser

```csharp
using Microsoft.AspNetCore.Identity;

namespace HelpDeskHero.Api.Domain;

public sealed class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = default!;
    public OrganizationUnit? OrganizationUnit { get; set; }
}
```

### TicketType

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class TicketType
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = default!;
    public List<WorkflowDefinition> WorkflowDefinitions { get; set; } = [];
}
```

Przykłady: `INCIDENT`, `SERVICE_REQUEST`, `ACCESS_REQUEST`, `CHANGE_REQUEST`.

### WorkflowDefinition

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class WorkflowDefinition
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public int TicketTypeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = default!;
    public TicketType TicketType { get; set; } = default!;
    public List<WorkflowState> States { get; set; } = [];
    public List<WorkflowTransition> Transitions { get; set; } = [];
}
```

### WorkflowState

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class WorkflowState
{
    public int Id { get; set; }
    public int WorkflowDefinitionId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsStart { get; set; }
    public bool IsEnd { get; set; }
    public int SortOrder { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
}
```

### WorkflowTransition

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class WorkflowTransition
{
    public int Id { get; set; }
    public int WorkflowDefinitionId { get; set; }
    public int FromStateId { get; set; }
    public int ToStateId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? RequiredRole { get; set; }
    public bool IsActive { get; set; } = true;

    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    public WorkflowState FromState { get; set; } = default!;
    public WorkflowState ToState { get; set; } = default!;
}
```

### Ticket

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class Ticket
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public int TicketTypeId { get; set; }
    public int? WorkflowStateId { get; set; }

    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";

    public string CreatedByUserId { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public DateTime? DueFirstResponseAtUtc { get; set; }
    public DateTime? DueResolveAtUtc { get; set; }
    public DateTime? FirstRespondedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int EscalationLevel { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Tenant Tenant { get; set; } = default!;
    public OrganizationUnit? OrganizationUnit { get; set; }
    public TicketType TicketType { get; set; } = default!;
    public WorkflowState? WorkflowState { get; set; }

    public List<TicketComment> Comments { get; set; } = [];
    public List<TicketAttachment> Attachments { get; set; } = [];
    public List<TicketEscalation> Escalations { get; set; } = [];
}
```

Uwaga: w wersji produkcyjnej `Ticket.Status` nie jest głównym źródłem prawdy. Stan zgłoszenia wynika z `WorkflowStateId`. W DTO można nadal wystawiać `Status`, ale mapowany z `WorkflowState.Code` lub `WorkflowState.Name`.

### TicketComment

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Guid TenantId { get; set; }

    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = default!;
}
```

### TicketAttachment

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Guid TenantId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = default!;
}
```

### TicketSlaPolicy

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class TicketSlaPolicy
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public int TicketTypeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public int FirstResponseMinutes { get; set; }
    public int ResolveMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = default!;
    public TicketType TicketType { get; set; } = default!;
}
```

### TicketEscalation

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class TicketEscalation
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Guid TenantId { get; set; }

    public int EscalationLevel { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }
    public bool NotificationSent { get; set; }

    public Ticket Ticket { get; set; } = default!;
}
```

### KnowledgeArticle

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class KnowledgeArticle
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string Category { get; set; } = "General";

    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public Tenant Tenant { get; set; } = default!;
}
```

### WebhookSubscription

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class WebhookSubscription
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public string EventName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = default!;
}
```

Uwaga produkcyjna: trzymaj `SecretHash`, a nie jawny sekret webhooka.

### KpiSnapshot

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class KpiSnapshot
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }

    public DateOnly SnapshotDate { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int BreachedSlaTickets { get; set; }
    public decimal AverageResolutionHours { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = default!;
}
```

### RefreshToken

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Unknown";
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
```

### AuditLog

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class AuditLog
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
}
```

### UserNotification

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class UserNotification
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Unread";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
```

### OutboxMessage

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
```

---

## 3. ITenantProvider

Plik:

```text
src\HelpDeskHero.Api\Infrastructure\Tenancy\ITenantProvider.cs
```

```csharp
namespace HelpDeskHero.Api.Infrastructure.Tenancy;

public interface ITenantProvider
{
    Guid GetTenantId();
    string? GetTenantCode();
}
```

Implementacja produkcyjna czyta `tenant_id` z JWT claim. Na potrzeby migracji/design-time trzeba mieć wariant bez HTTP contextu.

---

## 4. AppDbContext - pełne DbSety

```csharp
using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketSlaPolicy> TicketSlaPolicies => Set<TicketSlaPolicy>();
    public DbSet<TicketEscalation> TicketEscalations => Set<TicketEscalation>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenants");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<OrganizationUnit>(b =>
        {
            b.ToTable("OrganizationUnits");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Type).HasMaxLength(50).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Name });
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<TicketType>(b =>
        {
            b.ToTable("TicketTypes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("WorkflowDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.TicketTypeId, x.IsDefault });
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<WorkflowState>(b =>
        {
            b.ToTable("WorkflowStates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.WorkflowDefinitionId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<WorkflowTransition>(b =>
        {
            b.ToTable("WorkflowTransitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.RequiredRole).HasMaxLength(100);
            b.HasIndex(x => new { x.WorkflowDefinitionId, x.FromStateId, x.ToStateId }).IsUnique();

            b.HasOne(x => x.FromState)
                .WithMany()
                .HasForeignKey(x => x.FromStateId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ToState)
                .WithMany()
                .HasForeignKey(x => x.ToStateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(b =>
        {
            b.ToTable("Tickets");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(30).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(30).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.TenantId, x.WorkflowStateId });
            b.HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<KnowledgeArticle>(b =>
        {
            b.ToTable("KnowledgeArticles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
            b.Property(x => x.ContentMarkdown).IsRequired();
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<WebhookSubscription>(b =>
        {
            b.ToTable("WebhookSubscriptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            b.Property(x => x.TargetUrl).HasMaxLength(1000).IsRequired();
            b.Property(x => x.SecretHash).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EventName });
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<KpiSnapshot>(b =>
        {
            b.ToTable("KpiSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.AverageResolutionHours).HasPrecision(10, 2);
            b.HasIndex(x => new { x.TenantId, x.SnapshotDate }).IsUnique();
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(200).IsRequired();
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.Error).HasMaxLength(4000);
            b.HasIndex(x => new { x.TenantId, x.ProcessedAtUtc, x.OccurredAtUtc });
            b.HasQueryFilter(x => x.TenantId == _tenantProvider.GetTenantId());
        });
    }
}
```

---

## 5. Global query filters

Globalne filtry multi-tenant są wygodne, ale wymagają dyscypliny.

Do zwykłej pracy używasz:

```csharp
_db.Tickets.ToListAsync(ct);
```

Do panelu administracyjnego / maintenance / migracji danych używasz świadomie:

```csharp
_db.Tickets
    .IgnoreQueryFilters()
    .Where(x => x.TenantId == tenantId)
    .ToListAsync(ct);
```

Nigdy nie używaj `IgnoreQueryFilters()` bez jawnego warunku `TenantId`, jeśli dane są wielotenantowe.

---

## 6. Migracje

```powershell
dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialProductionSchema `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --output-dir Infrastructure\Persistence\Migrations

dotnet ef database update `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj
```

---

## 7. Weryfikacja SQL

```sql
USE HelpDeskHeroDb;
GO

SELECT name
FROM sys.tables
WHERE name IN
(
    'Tenants',
    'OrganizationUnits',
    'TicketTypes',
    'WorkflowDefinitions',
    'WorkflowStates',
    'WorkflowTransitions',
    'Tickets',
    'KnowledgeArticles',
    'WebhookSubscriptions',
    'KpiSnapshots'
)
ORDER BY name;
GO
```

---

## 8. Decyzja projektowa

Od tej wersji projekt **nie jest już prostym CRUD-em na `Ticket`**.

`Ticket` jest centrum systemu, ale produkcyjny model obejmuje:

- izolację tenantów,
- strukturę organizacyjną,
- typy zgłoszeń,
- workflow,
- SLA,
- komentarze,
- załączniki,
- bazę wiedzy,
- webhooki,
- KPI,
- audyt,
- outbox,
- powiadomienia.

## Seed danych

Model produkcyjny wymaga danych startowych, ale nie trzymamy ich w jednym dużym `DbInitializer`.

Dane startowe są seedowane przez osobne klasy:

```text
IdentitySeedStep
TenantSeedStep
OrganizationUnitSeedStep
TicketTypeSeedStep
WorkflowSeedStep
TicketSlaPolicySeedStep
KnowledgeBaseSeedStep
WebhookSubscriptionSeedStep
KpiSeedStep
```

Każdy seed step jest idempotentny i uruchamiany przez `DatabaseSeeder`. W produkcji seeda uruchamia konto migracyjne `hdh_migrator`, a nie konto aplikacyjne `hdh_app`.

Szczegóły: `02E_HelpDeskHero_EFCore_Production_Seed_Data.md`.
