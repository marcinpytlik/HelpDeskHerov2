# 08 - SignalR, SLA, outbox, multi-tenant, workflow

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. SignalR

Hub:

```csharp
[Authorize]
public sealed class TicketHub : Hub
{
    public Task JoinTicketGroup(int ticketId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");

    public Task LeaveTicketGroup(int ticketId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
}
```

Endpoint:

```csharp
app.MapHub<TicketHub>("/hubs/tickets");
```

## 2. Outbox pattern

Każda ważna zmiana biznesowa zapisuje zdarzenie do `OutboxMessages` w tej samej transakcji co zmiana danych.

Przykład zdarzeń:

```text
ticket.created
ticket.updated
ticket.assigned
ticket.closed
ticket.sla_breached
comment.created
attachment.uploaded
```

Outbox processor publikuje zdarzenia do:

- SignalR,
- systemu powiadomień,
- webhooków,
- RabbitMQ.

## 3. SLA

Encje:

```csharp
public sealed class TicketSlaPolicy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public int FirstResponseMinutes { get; set; }
    public int ResolveMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TicketEscalation
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }
    public bool NotificationSent { get; set; }
}
```

Job SLA:

- szuka ticketów po terminie,
- podnosi `EscalationLevel`,
- zapisuje `TicketEscalation`,
- dodaje event do outboxa.

## 4. Multi-tenant

Model startowy:

```text
wspólna baza + wspólne tabele + TenantId + global query filters
```

Encje:

```csharp
public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
```

Tenant provider:

```csharp
public interface ITenantProvider
{
    Guid? GetTenantId();
}
```

Źródło tenantu:

```text
JWT claim: tenant_id
```

## 5. Workflow

Encje:

```text
TicketType
WorkflowDefinition
WorkflowState
WorkflowTransition
```

Przykład workflow dla Incident:

```text
New -> Triaged -> InProgress -> Resolved -> Closed
```

Przykład workflow dla Access Request:

```text
New -> ManagerApproval -> SecurityApproval -> Completed
```

## 6. Knowledge base

Encja:

```csharp
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
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
```

## 7. Webhooks

Encja:

```csharp
public sealed class WebhookSubscription
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
```

Webhook nie jest wysyłany z kontrolera. Robi to processor outboxa.

## 8. Finalna checklista

- [ ] SignalR działa po zalogowaniu.
- [ ] Ticket update emituje event do outboxa.
- [ ] Outbox processor publikuje event do SignalR.
- [ ] SLA job wykrywa naruszenia.
- [ ] Multi-tenant filtruje dane po `TenantId`.
- [ ] Workflow blokuje niedozwolone przejścia statusu.
- [ ] Webhook ma retry i log błędów.
