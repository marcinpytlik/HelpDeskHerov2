# 11 — Tenant isolation

## Cel

Użytkownik z jednego tenantu nie może czytać, edytować, eksportować ani pobierać plików innego tenantu.

## Zasady

```text
1. Każda encja biznesowa wymagająca izolacji ma TenantId.
2. TenantId nie pochodzi z request body.
3. TenantId pochodzi z claims / CurrentUser / TenantProvider.
4. Odczyty są chronione przez filtry.
5. Zapisy są chronione przez application services.
6. Testy integracyjne sprawdzają cross-tenant access.
7. SystemAdmin bypass jest jawny i audytowany.
```

## Encje z TenantId

```text
OrganizationUnit
ApplicationUser
Ticket
TicketType
WorkflowDefinition
KnowledgeArticle
WebhookSubscription
KpiSnapshot
AuditLog
UserNotification
TicketSlaPolicy
TicketAttachment
```

## ITenantProvider

```csharp
public interface ITenantProvider
{
    Guid GetTenantId();
    bool HasTenant { get; }
}
```

```csharp
public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool HasTenant =>
        _httpContextAccessor.HttpContext?.User?.HasClaim(c => c.Type == "tenant_id") == true;

    public Guid GetTenantId()
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("Missing tenant_id claim.");

        return Guid.Parse(value);
    }
}
```

## DTO nie przyjmuje TenantId

Źle:

```csharp
public sealed class CreateTicketDto
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

Dobrze:

```csharp
public sealed class CreateTicketDto
{
    public int TicketTypeId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

TenantId ustawiamy po stronie backendu:

```csharp
var tenantId = _tenantProvider.GetTenantId();

var ticket = new Ticket
{
    TenantId = tenantId,
    TicketTypeId = dto.TicketTypeId,
    Title = dto.Title
};
```

## Cross-tenant test

```csharp
[Fact]
public async Task UserCannotReadTicketFromOtherTenant()
{
    var tenantAClient = await _factory.CreateClientForTenantAsync("tenant-a");
    var ticketFromTenantB = await _factory.SeedTicketAsync("tenant-b");

    var response = await tenantAClient.GetAsync($"/api/tickets/{ticketFromTenantB.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

## Checklist

- [ ] Czy encja biznesowa ma TenantId?
- [ ] Czy DTO wejściowe nie przyjmuje TenantId?
- [ ] Czy TenantId jest pobierany z claims?
- [ ] Czy listy filtrują po TenantId?
- [ ] Czy update/delete sprawdzają TenantId?
- [ ] Czy pliki są chronione TenantId?
- [ ] Czy audyt zawiera TenantId?
- [ ] Czy testy sprawdzają cross-tenant read/update?
