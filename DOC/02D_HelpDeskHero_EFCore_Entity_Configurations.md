# HelpDeskHero — EF Core: osobne klasy konfiguracji encji

## 1. Cel zmiany

W wersji produkcyjnej `AppDbContext` nie powinien zawierać długiego, trudnego do utrzymania `OnModelCreating`, w którym konfigurujemy wszystkie encje w jednym miejscu.

Docelowo stosujemy wzorzec:

```text
Jedna encja = jedna klasa konfiguracji EF Core
```

Czyli zamiast:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Tenant>(...);
    modelBuilder.Entity<Ticket>(...);
    modelBuilder.Entity<WorkflowState>(...);
    modelBuilder.Entity<AuditLog>(...);
}
```

robimy:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

Dzięki temu:

- `AppDbContext` jest krótki,
- konfiguracja encji jest łatwa do znalezienia,
- każdą encję można omawiać osobno na szkoleniu,
- łatwiej testować i rozwijać model,
- zmniejszamy ryzyko konfliktów przy pracy zespołowej.

---

## 2. Docelowa struktura katalogów

W projekcie `HelpDeskHero.Api` dodajemy katalog:

```text
src
└── HelpDeskHero.Api
    └── Infrastructure
        └── Persistence
            ├── AppDbContext.cs
            └── Configurations
                ├── TenantConfiguration.cs
                ├── OrganizationUnitConfiguration.cs
                ├── ApplicationUserConfiguration.cs
                ├── RefreshTokenConfiguration.cs
                ├── TicketTypeConfiguration.cs
                ├── WorkflowDefinitionConfiguration.cs
                ├── WorkflowStateConfiguration.cs
                ├── WorkflowTransitionConfiguration.cs
                ├── TicketConfiguration.cs
                ├── TicketCommentConfiguration.cs
                ├── TicketAttachmentConfiguration.cs
                ├── TicketSlaPolicyConfiguration.cs
                ├── TicketEscalationConfiguration.cs
                ├── KnowledgeArticleConfiguration.cs
                ├── UserNotificationConfiguration.cs
                ├── AuditLogConfiguration.cs
                ├── OutboxMessageConfiguration.cs
                ├── WebhookSubscriptionConfiguration.cs
                └── KpiSnapshotConfiguration.cs
```

---

## 3. AppDbContext po poprawce

Plik:

```text
src\HelpDeskHero.Api\Infrastructure\Persistence\AppDbContext.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider? _tenantProvider;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public Guid CurrentTenantId =>
        _tenantProvider?.GetTenantId() ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketSlaPolicy> TicketSlaPolicies => Set<TicketSlaPolicy>();
    public DbSet<TicketEscalation> TicketEscalations => Set<TicketEscalation>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();

    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### Ważne

`base.OnModelCreating(modelBuilder)` zostaje, ponieważ dziedziczymy po `IdentityDbContext<ApplicationUser>`.

Bez tego konfiguracja tabel ASP.NET Identity może być niepełna albo błędna.

---

## 4. TenantConfiguration

Plik:

```text
src\HelpDeskHero.Api\Infrastructure\Persistence\Configurations\TenantConfiguration.cs
```

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

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

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}
```

---

## 5. OrganizationUnitConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("OrganizationUnits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(x => x.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}
```

---

## 6. ApplicationUserConfiguration

ASP.NET Identity konfiguruje dużo elementów automatycznie, ale własne pola użytkownika warto skonfigurować jawnie.

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.OrganizationUnitId);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.NormalizedUserName })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## 7. TicketTypeConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

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

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();
    }
}
```

---

## 8. WorkflowDefinitionConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TicketType>()
            .WithMany()
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.TicketTypeId, x.IsDefault });
    }
}
```

---

## 9. WorkflowStateConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

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

        builder.Property(x => x.IsStart)
            .IsRequired();

        builder.Property(x => x.IsEnd)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.HasOne<WorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.Code })
            .IsUnique();
    }
}
```

---

## 10. WorkflowTransitionConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RequiredRole)
            .HasMaxLength(100);

        builder.HasOne<WorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkflowState>()
            .WithMany()
            .HasForeignKey(x => x.FromStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowState>()
            .WithMany()
            .HasForeignKey(x => x.ToStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.WorkflowDefinitionId,
            x.FromStateId,
            x.ToStateId
        }).IsUnique();
    }
}
```

---

## 11. TicketConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

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
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TicketType>()
            .WithMany()
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowState>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.Number })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.WorkflowStateId });
        builder.HasIndex(x => new { x.TenantId, x.Priority });
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
```

### Uwaga o filtrowaniu tenantów

Jeżeli chcesz mieć globalny filtr po tenancie w konfiguracji encji, możesz użyć właściwości z `AppDbContext`, np.:

```csharp
builder.HasQueryFilter(x => !x.IsDeleted && x.TenantId == EF.Property<Guid>(x, "TenantId"));
```

Jednak w praktyce dla multi-tenant bezpieczniej jest przygotować filtr przez kontekst, np. w `AppDbContext` i konfigurację zależną od instancji kontekstu. Ten fragment warto dopracować ostrożnie, bo global query filter dla tenantów jest krytycznym elementem bezpieczeństwa.

W wersji szkoleniowej można najpierw pokazać:

```csharp
builder.HasQueryFilter(x => !x.IsDeleted);
```

A osobny rozdział poświęcić filtrowaniu tenantów.

---

## 12. TicketCommentConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAtUtc });
    }
}
```

---

## 13. TicketAttachmentConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.UploadedAtUtc)
            .IsRequired();

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## 14. RefreshTokenConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.DeviceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.DeviceName });
    }
}
```

---

## 15. TicketSlaPolicyConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class TicketSlaPolicyConfiguration : IEntityTypeConfiguration<TicketSlaPolicy>
{
    public void Configure(EntityTypeBuilder<TicketSlaPolicy> builder)
    {
        builder.ToTable("TicketSlaPolicies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FirstResponseMinutes)
            .IsRequired();

        builder.Property(x => x.ResolveMinutes)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Priority, x.IsActive });
    }
}
```

---

## 16. TicketEscalationConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class TicketEscalationConfiguration : IEntityTypeConfiguration<TicketEscalation>
{
    public void Configure(EntityTypeBuilder<TicketEscalation> builder)
    {
        builder.ToTable("TicketEscalations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EscalationLevel)
            .IsRequired();

        builder.Property(x => x.TriggeredAtUtc)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.NotificationSent)
            .IsRequired();

        builder.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TicketId, x.EscalationLevel });
    }
}
```

---

## 17. KnowledgeArticleConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ContentMarkdown)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsPublished)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Slug })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Category, x.IsPublished });
    }
}
```

---

## 18. UserNotificationConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Status, x.CreatedAtUtc });
    }
}
```

---

## 19. AuditLogConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450);

        builder.Property(x => x.UserName)
            .HasMaxLength(256);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.DetailsJson);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId });
        builder.HasIndex(x => new { x.TenantId, x.Action });
    }
}
```

---

## 20. OutboxMessageConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(4000);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.Type, x.OccurredAtUtc });
    }
}
```

---

## 21. WebhookSubscriptionConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TargetUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Secret)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.EventName, x.IsActive });
    }
}
```

---

## 22. KpiSnapshotConfiguration

```csharp
using HelpDeskHero.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Configurations;

public sealed class KpiSnapshotConfiguration : IEntityTypeConfiguration<KpiSnapshot>
{
    public void Configure(EntityTypeBuilder<KpiSnapshot> builder)
    {
        builder.ToTable("KpiSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotDate)
            .IsRequired();

        builder.Property(x => x.OpenTickets)
            .IsRequired();

        builder.Property(x => x.ClosedTickets)
            .IsRequired();

        builder.Property(x => x.OverdueTickets)
            .IsRequired();

        builder.Property(x => x.AvgResolutionMinutes)
            .HasPrecision(18, 2);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.SnapshotDate })
            .IsUnique();
    }
}
```

---

## 23. Przykładowe encje, jeśli brakuje właściwości

Żeby powyższe konfiguracje kompilowały się poprawnie, encje domenowe muszą mieć zgodne właściwości. Przykładowo `ApplicationUser` powinien mieć pola tenantowe:

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
}
```

`Ticket` powinien zawierać klucze do typu zgłoszenia i stanu workflow:

```csharp
namespace HelpDeskHero.Api.Domain;

public sealed class Ticket
{
    public int Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public int TicketTypeId { get; set; }
    public int WorkflowStateId { get; set; }

    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? AssignedToUserId { get; set; }

    public DateTime? DueFirstResponseAtUtc { get; set; }
    public DateTime? DueResolveAtUtc { get; set; }
    public DateTime? FirstRespondedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime? LastNotifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
```

---

## 24. Dlaczego nie trzymać konfiguracji w encjach domenowych

W tym projekcie encje domenowe powinny być możliwie czyste.

Nie robimy tego:

```csharp
public sealed class Ticket
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        // nie tutaj
    }
}
```

Konfiguracja EF Core należy do warstwy:

```text
Infrastructure/Persistence/Configurations
```

Domena opisuje pojęcia biznesowe, a infrastruktura opisuje sposób mapowania tych pojęć do SQL Server.

---

## 25. Rejestracja DbContext zostaje bez zmian

W `Program.cs` nadal rejestrujemy kontekst normalnie:

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var queryInterceptor = sp.GetRequiredService<EfQueryPerformanceInterceptor>();

    options.UseSqlServer(connectionString);
    options.AddInterceptors(queryInterceptor);
});
```

Zmienia się tylko to, że `OnModelCreating` nie zawiera już ręcznej konfiguracji każdej encji.

---

## 26. Checklist wdrożeniowy

Po refaktoryzacji wykonaj:

```powershell
dotnet build .\HelpDeskHero.sln
```

Następnie dodaj migrację:

```powershell
dotnet ef migrations add RefactorEntityConfigurations `
  --project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --startup-project .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj `
  --context AppDbContext
```

Jeżeli migracja zawiera masowe `DropTable` / `DropColumn`, zatrzymaj się i sprawdź konfigurację nazw tabel, kluczy i relacji.

Dla istniejącej bazy taka refaktoryzacja powinna wygenerować pustą albo minimalną migrację, jeśli konfiguracja odpowiada poprzedniemu modelowi.

---

## 27. Najważniejsza zasada do pokazania na nagraniu

```text
DbContext ma zarządzać modelem, ale nie powinien być śmietnikiem konfiguracji całej domeny.
```

Dlatego produkcyjnie:

```text
AppDbContext
  -> DbSety
  -> ApplyConfigurationsFromAssembly

Configurations
  -> jedna klasa konfiguracji na jedną encję
```

To jest czytelne, skalowalne i bardzo dobrze pokazuje profesjonalne użycie EF Core w aplikacji biznesowej.
