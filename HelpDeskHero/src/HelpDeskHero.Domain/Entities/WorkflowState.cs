using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class WorkflowState : AuditableEntity
{
    public int WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool IsStart { get; set; }
    public bool IsFinal { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<WorkflowTransition> OutgoingTransitions { get; set; } = [];
    public ICollection<WorkflowTransition> IncomingTransitions { get; set; } = [];
}