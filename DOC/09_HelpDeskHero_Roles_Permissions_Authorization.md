# 09 — Role, permissions i autoryzacja

## Cel

W aplikacji produkcyjnej nie wystarczy prosty model `Admin/User`. Rozdzielamy:

- role,
- permissions,
- policies,
- tenant context,
- dostęp do własnych danych,
- dostęp administracyjny.

## Role

```text
SystemAdmin
TenantAdmin
SupportManager
SupportAgent
Requester
Auditor
KnowledgeBaseEditor
IntegrationAdmin
ReadOnlyViewer
```

## Permissions

### Tickets

```text
tickets.read
tickets.read_all
tickets.create
tickets.update
tickets.assign
tickets.change_state
tickets.delete
tickets.restore
tickets.export
tickets.comment
tickets.close
```

### Pozostałe

```text
audit.read
audit.export
workflow.read
workflow.manage
workflow.publish
sla.read
sla.manage
kb.read
kb.manage
kb.publish
users.read
users.manage
tenants.read
tenants.manage
organization_units.manage
reports.read
reports.export
webhooks.read
webhooks.manage
outbox.read
outbox.retry
```

## Stałe w kodzie

```csharp
public static class Permissions
{
    public static class Tickets
    {
        public const string Read = "tickets.read";
        public const string Create = "tickets.create";
        public const string Update = "tickets.update";
        public const string Assign = "tickets.assign";
        public const string ChangeState = "tickets.change_state";
        public const string Delete = "tickets.delete";
        public const string Restore = "tickets.restore";
        public const string Export = "tickets.export";
    }

    public static class Audit
    {
        public const string Read = "audit.read";
    }

    public static class Workflow
    {
        public const string Manage = "workflow.manage";
    }

    public static class Tenants
    {
        public const string Manage = "tenants.manage";
    }
}
```

## Policy names

```csharp
public static class PolicyNames
{
    public const string TicketsRead = "Tickets.Read";
    public const string TicketsCreate = "Tickets.Create";
    public const string TicketsUpdate = "Tickets.Update";
    public const string TicketsAssign = "Tickets.Assign";
    public const string TicketsChangeState = "Tickets.ChangeState";
    public const string TicketsExport = "Tickets.Export";
    public const string AuditRead = "Audit.Read";
    public const string WorkflowManage = "Workflow.Manage";
    public const string TenantsManage = "Tenants.Manage";
}
```

## Rejestracja policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.TicketsRead,
        p => p.RequireClaim("permission", Permissions.Tickets.Read));

    options.AddPolicy(PolicyNames.TicketsCreate,
        p => p.RequireClaim("permission", Permissions.Tickets.Create));

    options.AddPolicy(PolicyNames.TicketsUpdate,
        p => p.RequireClaim("permission", Permissions.Tickets.Update));

    options.AddPolicy(PolicyNames.TicketsExport,
        p => p.RequireClaim("permission", Permissions.Tickets.Export));

    options.AddPolicy(PolicyNames.AuditRead,
        p => p.RequireClaim("permission", Permissions.Audit.Read));
});
```

## Użycie

```csharp
[HttpGet]
[Authorize(Policy = PolicyNames.TicketsRead)]
public async Task<ActionResult<PagedResultDto<TicketListItemDto>>> Search(
    [FromQuery] TicketQueryDto query,
    CancellationToken ct)
{
    var result = await _tickets.SearchAsync(query, ct);
    return Ok(result);
}
```

## Claims w JWT

```text
sub
name
tenant_id
role
permission
```

## Checklist

- [ ] Czy każda funkcja API ma policy?
- [ ] Czy permissions są stałymi w kodzie?
- [ ] Czy JWT zawiera tylko potrzebne claims?
- [ ] Czy tenant id pochodzi z claims?
- [ ] Czy testy sprawdzają 403 dla braku uprawnień?
