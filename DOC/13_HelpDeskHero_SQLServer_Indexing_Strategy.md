# 13 — Strategia indeksów SQL Server

## Cel

Indeksy projektujemy pod zapytania i ekrany aplikacji, nie pod same tabele.

## Scenariusze

```text
Lista ticketów
Moje tickety
Tickety według SLA
Outbox
Audit
Knowledge Base
Webhook subscriptions
```

## Tickets — lista według stanu

```sql
CREATE INDEX IX_Tickets_Tenant_WorkflowState_CreatedAt
ON dbo.Tickets (TenantId, WorkflowStateId, CreatedAtUtc DESC)
INCLUDE (Number, Title, Priority, AssignedToUserId, TicketTypeId)
WHERE IsDeleted = 0;
```

## Tickets — moje tickety

```sql
CREATE INDEX IX_Tickets_Tenant_AssignedTo_CreatedAt
ON dbo.Tickets (TenantId, AssignedToUserId, CreatedAtUtc DESC)
INCLUDE (Number, Title, Priority, WorkflowStateId, TicketTypeId)
WHERE IsDeleted = 0;
```

## Tickets — SLA

```sql
CREATE INDEX IX_Tickets_Tenant_DueResolve
ON dbo.Tickets (TenantId, DueResolveAtUtc)
INCLUDE (Number, Title, Priority, WorkflowStateId, AssignedToUserId)
WHERE IsDeleted = 0 AND ResolvedAtUtc IS NULL;
```

## Unikalny numer ticketu

```sql
CREATE UNIQUE INDEX UX_Tickets_Tenant_Number
ON dbo.Tickets (TenantId, Number);
```

## Outbox

```sql
CREATE INDEX IX_OutboxMessages_Unprocessed
ON dbo.OutboxMessages (ProcessedAtUtc, OccurredAtUtc)
INCLUDE (Type, RetryCount)
WHERE ProcessedAtUtc IS NULL;
```

## Audit

```sql
CREATE INDEX IX_AuditLogs_Tenant_CreatedAt
ON dbo.AuditLogs (TenantId, CreatedAtUtc DESC)
INCLUDE (Action, EntityName, EntityId, UserId, UserName);
```

## Knowledge Base

```sql
CREATE UNIQUE INDEX UX_KnowledgeArticles_Tenant_Slug
ON dbo.KnowledgeArticles (TenantId, Slug);
```

## EF Core

```csharp
b.HasIndex(x => new { x.TenantId, x.WorkflowStateId, x.CreatedAtUtc })
    .HasDatabaseName("IX_Tickets_Tenant_WorkflowState_CreatedAt")
    .HasFilter("[IsDeleted] = 0")
    .IncludeProperties(x => new
    {
        x.Number,
        x.Title,
        x.Priority,
        x.AssignedToUserId,
        x.TicketTypeId
    });
```

## Checklist

- [ ] Czy indeksy odpowiadają endpointom?
- [ ] Czy lista ticketów ma indeks po TenantId?
- [ ] Czy outbox ma filtered index po ProcessedAtUtc IS NULL?
- [ ] Czy audit ma indeks po TenantId i CreatedAtUtc?
- [ ] Czy soft delete jest uwzględniony w filtered indexes?
