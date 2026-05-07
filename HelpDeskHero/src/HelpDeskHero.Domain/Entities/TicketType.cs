using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class TicketType : AuditableEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}