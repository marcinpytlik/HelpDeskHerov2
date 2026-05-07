# 04 - Tickets, audit, soft delete i concurrency

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Endpointy Tickets

```text
GET    /api/tickets
GET    /api/tickets/{id}
POST   /api/tickets
PUT    /api/tickets/{id}
DELETE /api/tickets/{id}
POST   /api/tickets/{id}/restore
GET    /api/tickets/export
```

## 2. DTO

```csharp
public sealed class TicketQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string SortBy { get; set; } = "CreatedAtUtc";
    public bool Desc { get; set; } = true;
}

public sealed class TicketDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string RowVersionBase64 { get; set; } = string.Empty;
}

public sealed class UpdateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public string RowVersionBase64 { get; set; } = string.Empty;
}
```

## 3. Soft delete

Kasowanie nie usuwa rekordu fizycznie:

```csharp
ticket.IsDeleted = true;
ticket.DeletedAtUtc = DateTime.UtcNow;
ticket.DeletedByUserId = userId;
```

Restore:

```csharp
var ticket = await _db.Tickets.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
ticket.IsDeleted = false;
ticket.DeletedAtUtc = null;
ticket.DeletedByUserId = null;
```

## 4. Concurrency

Przed zapisem ustawiamy oryginalną wartość `RowVersion`:

```csharp
_db.Entry(ticket).Property(x => x.RowVersion).OriginalValue =
    Convert.FromBase64String(dto.RowVersionBase64);
```

Przy konflikcie zwracamy `409 Conflict`.

## 5. Audit

Minimalny model:

```csharp
public sealed class AuditLog
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
}
```

Operacje do audytu:

- `TicketCreated`,
- `TicketUpdated`,
- `TicketSoftDeleted`,
- `TicketRestored`,
- `TicketCommentAdded`,
- `TicketAttachmentUploaded`,
- `TicketAssigned`,
- `TicketClosed`.

## 6. Rejestracja serwisów

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
```
