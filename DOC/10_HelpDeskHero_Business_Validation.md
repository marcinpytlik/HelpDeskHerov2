# 10 — Pełna walidacja biznesowa

## Cel

Walidacja w produkcyjnej aplikacji musi mieć kilka poziomów:

- walidacja DTO,
- walidacja aplikacyjna,
- walidacja biznesowa,
- walidacja bezpieczeństwa,
- walidacja tenant isolation,
- walidacja workflow.

## Przykłady reguł

```text
Nie można zamknąć ticketu bez rozwiązania.
Nie można przejść do stanu niedozwolonego przez workflow.
Nie można dodać załącznika do zamkniętego ticketu.
Nie można zmienić TicketType po rozpoczęciu workflow.
Nie można usunąć ticketu, jeśli ma aktywną eskalację.
Nie można przypisać ticketu do użytkownika z innego tenantu.
```

## Struktura

```text
Application
├── Validation
│   ├── BusinessRuleException.cs
│   └── BusinessErrorCodes.cs
├── Tickets
│   └── Validation
│       ├── ITicketBusinessValidator.cs
│       └── TicketBusinessValidator.cs
└── Workflow
    └── Validation
        ├── IWorkflowTransitionValidator.cs
        └── WorkflowTransitionValidator.cs
```

## BusinessRuleException

```csharp
public sealed class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
```

## Kody błędów

```csharp
public static class BusinessErrorCodes
{
    public const string TicketClosed = "TICKET_CLOSED";
    public const string InvalidWorkflowTransition = "WORKFLOW_TRANSITION_NOT_ALLOWED";
    public const string TenantMismatch = "TENANT_MISMATCH";
    public const string AttachmentNotAllowed = "ATTACHMENT_NOT_ALLOWED";
    public const string CannotDeleteEscalatedTicket = "CANNOT_DELETE_ESCALATED_TICKET";
    public const string CannotChangeTicketType = "CANNOT_CHANGE_TICKET_TYPE";
    public const string AssigneeNotFound = "ASSIGNEE_NOT_FOUND";
}
```

## TicketBusinessValidator

```csharp
public interface ITicketBusinessValidator
{
    Task ValidateCreateAsync(CreateTicketDto dto, CancellationToken ct);
    Task ValidateUpdateAsync(Ticket ticket, UpdateTicketDto dto, CancellationToken ct);
    Task ValidateDeleteAsync(Ticket ticket, CancellationToken ct);
    Task ValidateAssignAsync(Ticket ticket, string assignedToUserId, CancellationToken ct);
}
```

## WorkflowTransitionValidator

```csharp
public interface IWorkflowTransitionValidator
{
    Task ValidateTransitionAsync(Ticket ticket, int targetWorkflowStateId, CancellationToken ct);
}
```

```csharp
public sealed class WorkflowTransitionValidator : IWorkflowTransitionValidator
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public WorkflowTransitionValidator(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task ValidateTransitionAsync(Ticket ticket, int targetWorkflowStateId, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var allowed = await _db.WorkflowTransitions.AnyAsync(x =>
            x.WorkflowDefinition.TenantId == tenantId &&
            x.WorkflowDefinition.TicketTypeId == ticket.TicketTypeId &&
            x.FromStateId == ticket.WorkflowStateId &&
            x.ToStateId == targetWorkflowStateId,
            ct);

        if (!allowed)
            throw new BusinessRuleException(
                BusinessErrorCodes.InvalidWorkflowTransition,
                "Workflow transition is not allowed.");
    }
}
```

## Checklist

- [ ] Czy walidacja DTO jest oddzielona od walidacji biznesowej?
- [ ] Czy reguły workflow nie są w kontrolerach?
- [ ] Czy wyjątki biznesowe mają stabilne kody?
- [ ] Czy testy sprawdzają błędne przejścia workflow?
- [ ] Czy testy sprawdzają przypisanie użytkownika z innego tenantu?
