using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class WorkflowDefinition : AuditableEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public int TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    public ICollection<WorkflowState> States { get; set; } = [];
    public ICollection<WorkflowTransition> Transitions { get; set; } = [];
}
