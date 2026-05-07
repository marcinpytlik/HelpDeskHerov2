using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class WorkflowTransition : AuditableEntity
{
    public int WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    public int FromStateId { get; set; }
    public WorkflowState FromState { get; set; } = default!;

    public int ToStateId { get; set; }
    public WorkflowState ToState { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool RequiresComment { get; set; }
    public bool IsActive { get; set; } = true;
}