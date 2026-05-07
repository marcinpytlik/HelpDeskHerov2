using HelpDeskHero.Domain.Common;
using HelpDeskHero.Domain.Enums;

namespace HelpDeskHero.Domain.Entities;

public sealed class Ticket : AuditableEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public int? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public int TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = default!;

    public int WorkflowStateId { get; set; }
    public WorkflowState WorkflowState { get; set; } = default!;

    public string Number { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public string CreatedByUserId { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }

    public DateTime? DueResponseAtUtc { get; set; }
    public DateTime? DueResolveAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}
