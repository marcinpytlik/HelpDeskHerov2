# 03A - Warstwa Application Services i cienkie kontrolery

> Wersja: **HelpDeskHero Production Edition v3**  
> Cel: przeniesienie logiki przypadków użycia z kontrolerów do serwisów aplikacyjnych.

---

## 1. Decyzja architektoniczna

W wersji produkcyjnej kontrolery HTTP mają być **cienkie**.

Kontroler odpowiada za:

- routing HTTP,
- autoryzację endpointu,
- przyjęcie DTO z requestu,
- wywołanie serwisu aplikacyjnego,
- zwrócenie odpowiedniego `ActionResult`.

Kontroler nie powinien zawierać:

- logiki tworzenia numerów ticketów,
- reguł workflow,
- ręcznego filtrowania po tenantach,
- logiki audytu,
- zapisu outboxa,
- wysyłki powiadomień,
- obsługi plików,
- zapytań dashboardowych,
- bezpośredniego orkiestracyjnego użycia Hangfire/SignalR.

To wszystko trafia do warstwy `Application`.

---

## 2. Docelowy układ katalogów API

```text
src
└── HelpDeskHero.Api
    ├── Controllers
    │   ├── AuthController.cs
    │   ├── TicketsController.cs
    │   ├── DashboardController.cs
    │   ├── KnowledgeBaseController.cs
    │   ├── WebhookSubscriptionsController.cs
    │   ├── WorkflowController.cs
    │   └── TenantAdminController.cs
    │
    ├── Application
    │   ├── Auth
    │   │   ├── IAuthApplicationService.cs
    │   │   ├── AuthApplicationService.cs
    │   │   ├── ITokenService.cs
    │   │   └── TokenService.cs
    │   │
    │   ├── Tickets
    │   │   ├── ITicketApplicationService.cs
    │   │   ├── TicketApplicationService.cs
    │   │   ├── ITicketNumberGenerator.cs
    │   │   └── TicketNumberGenerator.cs
    │   │
    │   ├── Workflow
    │   │   ├── IWorkflowApplicationService.cs
    │   │   └── WorkflowApplicationService.cs
    │   │
    │   ├── Audit
    │   │   ├── IAuditLogService.cs
    │   │   └── AuditLogService.cs
    │   │
    │   ├── Outbox
    │   │   ├── IOutboxService.cs
    │   │   └── OutboxService.cs
    │   │
    │   ├── Dashboard
    │   │   ├── IDashboardApplicationService.cs
    │   │   └── DashboardApplicationService.cs
    │   │
    │   ├── KnowledgeBase
    │   │   ├── IKnowledgeBaseApplicationService.cs
    │   │   └── KnowledgeBaseApplicationService.cs
    │   │
    │   ├── Webhooks
    │   │   ├── IWebhookSubscriptionService.cs
    │   │   └── WebhookSubscriptionService.cs
    │   │
    │   ├── Kpi
    │   │   ├── IKpiApplicationService.cs
    │   │   └── KpiApplicationService.cs
    │   │
    │   └── Common
    │       ├── ICurrentUserService.cs
    │       ├── CurrentUserService.cs
    │       ├── IClock.cs
    │       └── SystemClock.cs
    │
    ├── Domain
    ├── Infrastructure
    ├── BackgroundJobs
    ├── Hubs
    ├── Middleware
    └── Program.cs
```

---

## 3. Interfejs serwisu ticketów

Plik:

```text
src\HelpDeskHero.Api\Application\Tickets\ITicketApplicationService.cs
```

```csharp
using HelpDeskHero.Shared.Contracts.Common;
using HelpDeskHero.Shared.Contracts.Tickets;

namespace HelpDeskHero.Api.Application.Tickets;

public interface ITicketApplicationService
{
    Task<TicketDetailsDto> CreateAsync(CreateTicketDto dto, CancellationToken ct = default);
    Task<TicketDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResultDto<TicketListItemDto>> SearchAsync(TicketQueryDto query, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateTicketDto dto, CancellationToken ct = default);
    Task SoftDeleteAsync(int id, CancellationToken ct = default);
    Task RestoreAsync(int id, CancellationToken ct = default);
    Task AssignAsync(int id, AssignTicketDto dto, CancellationToken ct = default);
    Task ChangeWorkflowStateAsync(int id, ChangeTicketStateDto dto, CancellationToken ct = default);
    Task<byte[]> ExportCsvAsync(TicketExportQueryDto query, CancellationToken ct = default);
}
```

---

## 4. Lekki kontroler ticketów

Plik:

```text
src\HelpDeskHero.Api\Controllers\TicketsController.cs
```

```csharp
using HelpDeskHero.Api.Application.Tickets;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskHero.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketApplicationService _tickets;

    public TicketsController(ITicketApplicationService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet]
    public async Task<ActionResult> Search([FromQuery] TicketQueryDto query, CancellationToken ct)
    {
        var result = await _tickets.SearchAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailsDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _tickets.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<ActionResult<TicketDetailsDto>> Create(CreateTicketDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> Update(int id, UpdateTicketDto dto, CancellationToken ct)
    {
        await _tickets.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken ct)
    {
        await _tickets.SoftDeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        await _tickets.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> Assign(int id, AssignTicketDto dto, CancellationToken ct)
    {
        await _tickets.AssignAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/workflow-state")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> ChangeWorkflowState(int id, ChangeTicketStateDto dto, CancellationToken ct)
    {
        await _tickets.ChangeWorkflowStateAsync(id, dto, ct);
        return NoContent();
    }
}
```

---

## 5. Przykład serwisu aplikacyjnego ticketów

Plik:

```text
src\HelpDeskHero.Api\Application\Tickets\TicketApplicationService.cs
```

```csharp
using HelpDeskHero.Api.Application.Audit;
using HelpDeskHero.Api.Application.Common;
using HelpDeskHero.Api.Application.Outbox;
using HelpDeskHero.Api.Application.Workflow;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Api.Infrastructure.Tenancy;
using HelpDeskHero.Shared.Contracts.Common;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Application.Tickets;

public sealed class TicketApplicationService : ITicketApplicationService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketNumberGenerator _numberGenerator;
    private readonly IWorkflowApplicationService _workflow;
    private readonly IAuditLogService _audit;
    private readonly IOutboxService _outbox;
    private readonly IClock _clock;

    public TicketApplicationService(
        AppDbContext db,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUser,
        ITicketNumberGenerator numberGenerator,
        IWorkflowApplicationService workflow,
        IAuditLogService audit,
        IOutboxService outbox,
        IClock clock)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _numberGenerator = numberGenerator;
        _workflow = workflow;
        _audit = audit;
        _outbox = outbox;
        _clock = clock;
    }

    public async Task<TicketDetailsDto> CreateAsync(CreateTicketDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var now = _clock.UtcNow;

        var ticketType = await _db.TicketTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == dto.TicketTypeId && x.TenantId == tenantId, ct);

        if (ticketType is null)
            throw new InvalidOperationException("Ticket type not found for current tenant.");

        var startState = await _workflow.GetStartStateAsync(tenantId, dto.TicketTypeId, ct);

        var ticket = new Domain.Ticket
        {
            TenantId = tenantId,
            OrganizationUnitId = dto.OrganizationUnitId,
            TicketTypeId = dto.TicketTypeId,
            WorkflowStateId = startState.Id,
            Number = await _numberGenerator.GenerateAsync(tenantId, ct),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority,
            CreatedAtUtc = now,
            CreatedByUserId = _currentUser.UserId
        };

        _db.Tickets.Add(ticket);

        await _audit.AddAsync(
            action: "TicketCreated",
            entityName: "Ticket",
            entityId: ticket.Number,
            details: new { ticket.Title, ticket.Priority, ticket.TicketTypeId },
            ct);

        await _outbox.AddAsync(
            type: "ticket.created",
            payload: new
            {
                ticket.TenantId,
                ticket.Id,
                ticket.Number,
                ticket.Title,
                ticket.Priority,
                WorkflowState = startState.Code
            },
            ct);

        await _db.SaveChangesAsync(ct);

        return new TicketDetailsDto
        {
            Id = ticket.Id,
            Number = ticket.Number,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            WorkflowState = startState.Name,
            CreatedAtUtc = ticket.CreatedAtUtc
        };
    }

    public async Task<TicketDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Tickets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TicketDetailsDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Description = x.Description,
                Priority = x.Priority,
                WorkflowState = x.WorkflowState.Name,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
                RowVersionBase64 = Convert.ToBase64String(x.RowVersion)
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task<PagedResultDto<TicketListItemDto>> SearchAsync(TicketQueryDto query, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Number.Contains(search) || x.Title.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
            q = q.Where(x => x.Priority == query.Priority);

        if (!string.IsNullOrWhiteSpace(query.WorkflowStateCode))
            q = q.Where(x => x.WorkflowState.Code == query.WorkflowStateCode);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new TicketListItemDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Priority = x.Priority,
                WorkflowState = x.WorkflowState.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return new PagedResultDto<TicketListItemDto>
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            Items = items
        };
    }

    public Task UpdateAsync(int id, UpdateTicketDto dto, CancellationToken ct = default)
    {
        // Implementacja w pliku 04: walidacja RowVersion, zmiana pól, audit, outbox, SaveChanges.
        throw new NotImplementedException();
    }

    public Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        // Implementacja w pliku 04: ustawienie IsDeleted, DeletedAtUtc, DeletedByUserId, audit, outbox.
        throw new NotImplementedException();
    }

    public Task RestoreAsync(int id, CancellationToken ct = default)
    {
        // Implementacja w pliku 04: IgnoreQueryFilters, odtworzenie soft delete, audit, outbox.
        throw new NotImplementedException();
    }

    public Task AssignAsync(int id, AssignTicketDto dto, CancellationToken ct = default)
    {
        // Implementacja: przypisanie agenta, audit, outbox ticket.assigned.
        throw new NotImplementedException();
    }

    public Task ChangeWorkflowStateAsync(int id, ChangeTicketStateDto dto, CancellationToken ct = default)
    {
        // Implementacja: walidacja przejścia workflow, aktualizacja WorkflowStateId, audit, outbox.
        throw new NotImplementedException();
    }

    public Task<byte[]> ExportCsvAsync(TicketExportQueryDto query, CancellationToken ct = default)
    {
        // Implementacja w pliku 06: eksport CSV z filtrowaniem.
        throw new NotImplementedException();
    }
}
```

---

## 6. Workflow jako osobny serwis

```csharp
namespace HelpDeskHero.Api.Application.Workflow;

public interface IWorkflowApplicationService
{
    Task<WorkflowState> GetStartStateAsync(Guid tenantId, int ticketTypeId, CancellationToken ct = default);
    Task ValidateTransitionAsync(Guid tenantId, int workflowDefinitionId, int fromStateId, int toStateId, CancellationToken ct = default);
}
```

Kontroler ticketów nie powinien sam sprawdzać przejść workflow. Robi to `WorkflowApplicationService`.

---

## 7. Audit jako osobny serwis

```csharp
namespace HelpDeskHero.Api.Application.Audit;

public interface IAuditLogService
{
    Task AddAsync(string action, string entityName, string entityId, object? details, CancellationToken ct = default);
}
```

Serwis audytu powinien automatycznie pobierać:

- `TenantId`,
- `UserId`,
- `UserName`,
- `IpAddress`,
- `TraceId`,
- czas UTC.

---

## 8. Outbox jako osobny serwis

```csharp
namespace HelpDeskHero.Api.Application.Outbox;

public interface IOutboxService
{
    Task AddAsync(string type, object payload, CancellationToken ct = default);
}
```

Serwis aplikacyjny zapisuje komunikat do outboxa w tej samej transakcji co zmiana biznesowa.

Publikacja do SignalR, emaili, webhooków albo RabbitMQ odbywa się później przez worker/Background Job.

---

## 9. Common services

### ICurrentUserService

```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```

### IClock

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

Użycie `IClock` ułatwia testy i eliminuje rozsiane `DateTime.UtcNow`.

---

## 10. Rejestracja w Program.cs

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IClock, SystemClock>();

builder.Services.AddScoped<ITicketApplicationService, TicketApplicationService>();
builder.Services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
builder.Services.AddScoped<IWorkflowApplicationService, WorkflowApplicationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();

builder.Services.AddScoped<IDashboardApplicationService, DashboardApplicationService>();
builder.Services.AddScoped<IKnowledgeBaseApplicationService, KnowledgeBaseApplicationService>();
builder.Services.AddScoped<IWebhookSubscriptionService, WebhookSubscriptionService>();
builder.Services.AddScoped<IKpiApplicationService, KpiApplicationService>();
```

---

## 11. Zasada końcowa

W HelpDeskHero Production Edition obowiązuje reguła:

```text
Controllers -> Application Services -> Domain + Infrastructure
```

Nie robimy:

```text
Controllers -> DbContext + Audit + Outbox + Hangfire + SignalR + FileSystem
```

To jest właśnie poprawka, której brakowało w poprzedniej paczce.
