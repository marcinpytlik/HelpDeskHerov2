using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class Tenant : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationUnit> OrganizationUnits { get; set; } = [];
    public ICollection<TicketType> TicketTypes { get; set; } = [];
    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
