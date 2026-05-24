using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class WorkflowSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 40;

    public WorkflowSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var incidentType = await _db.TicketTypes
            .SingleAsync(
                x => x.TenantId == tenant.Id && x.Code == "INCIDENT",
                cancellationToken);

        var workflowExists = await _db.WorkflowDefinitions
            .AnyAsync(
                x => x.TenantId == tenant.Id
                     && x.TicketTypeId == incidentType.Id
                     && x.Code == "INCIDENT_DEFAULT",
                cancellationToken);

        if (workflowExists)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var workflow = new WorkflowDefinition
        {
            TenantId = tenant.Id,
            TicketTypeId = incidentType.Id,
            Code = "INCIDENT_DEFAULT",
            Name = "Incident Default Workflow",
            IsActive = true,
            IsDefault = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var newState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "NEW",
            Name = "New",
            IsStart = true,
            IsFinal = false,
            SortOrder = 10,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var triagedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "TRIAGED",
            Name = "Triaged",
            IsStart = false,
            IsFinal = false,
            SortOrder = 20,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var inProgressState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "IN_PROGRESS",
            Name = "In Progress",
            IsStart = false,
            IsFinal = false,
            SortOrder = 30,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var resolvedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "RESOLVED",
            Name = "Resolved",
            IsStart = false,
            IsFinal = false,
            SortOrder = 40,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var closedState = new WorkflowState
        {
            WorkflowDefinition = workflow,
            Code = "CLOSED",
            Name = "Closed",
            IsStart = false,
            IsFinal = true,
            SortOrder = 50,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        workflow.States.Add(newState);
        workflow.States.Add(triagedState);
        workflow.States.Add(inProgressState);
        workflow.States.Add(resolvedState);
        workflow.States.Add(closedState);

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = newState,
            ToState = triagedState,
            Code = "NEW_TO_TRIAGED",
            Name = "New -> Triaged",
            RequiresComment = false,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = triagedState,
            ToState = inProgressState,
            Code = "TRIAGED_TO_IN_PROGRESS",
            Name = "Triaged -> In Progress",
            RequiresComment = false,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = inProgressState,
            ToState = resolvedState,
            Code = "IN_PROGRESS_TO_RESOLVED",
            Name = "In Progress -> Resolved",
            RequiresComment = true,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        });

        workflow.Transitions.Add(new WorkflowTransition
        {
            WorkflowDefinition = workflow,
            FromState = resolvedState,
            ToState = closedState,
            Code = "RESOLVED_TO_CLOSED",
            Name = "Resolved -> Closed",
            RequiresComment = false,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        });

        _db.WorkflowDefinitions.Add(workflow);

        await _db.SaveChangesAsync(cancellationToken);
    }
}