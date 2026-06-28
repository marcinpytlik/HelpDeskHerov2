using HelpDeskHero.Application.Common;
using HelpDeskHero.Application.Security;
using HelpDeskHero.Application.Tickets.Dtos;
using HelpDeskHero.Domain.Entities;
using HelpDeskHero.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Application.Tickets;

public sealed class TicketApplicationService : ITicketApplicationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly ITicketNumberGenerator _numberGenerator;
    private readonly TicketBusinessValidator _validator;

    public TicketApplicationService(
    IAppDbContext db,
    ICurrentTenantProvider tenantProvider,
    ITicketNumberGenerator numberGenerator,
    TicketBusinessValidator validator)
{
    _db = db;
    _tenantProvider = tenantProvider;
    _numberGenerator = numberGenerator;
    _validator = validator;
}

    public async Task<TicketDetailsDto> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        _validator.ValidateCreate(request);

        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var ticketType = await _db.TicketTypes
            .SingleOrDefaultAsync(
                x => x.Id == request.TicketTypeId
                     && x.TenantId == tenantId
                     && x.IsActive,
                cancellationToken);

        if (ticketType is null)
        {
            //throw new InvalidOperationException("Ticket type was not found.");
            throw new BusinessRuleException(
                "TicketTypeNotFound",
                "Ticket type was not found.");
        }

        if (request.OrganizationUnitId is not null)
        {
            var organizationUnitExists = await _db.OrganizationUnits
                .AnyAsync(
                    x => x.Id == request.OrganizationUnitId
                         && x.TenantId == tenantId
                         && x.IsActive,
                    cancellationToken);

            if (!organizationUnitExists)
            {
                //throw new InvalidOperationException("Organization unit was not found.");
                throw new BusinessRuleException(
                    "OrganizationUnitNotFound",
                    "Organization unit was not found.");
            }
        }

        var workflow = await _db.WorkflowDefinitions
            .Include(x => x.States)
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId
                     && x.TicketTypeId == ticketType.Id
                     && x.IsDefault
                     && x.IsActive,
                cancellationToken);

        if (workflow is null)
        {
            //throw new InvalidOperationException("Default workflow was not found.");
            throw new BusinessRuleException(
                "WorkflowNotFound",
                "Default workflow was not found.");
        }

        var startState = workflow.States.SingleOrDefault(x => x.IsStart);

        if (startState is null)
        {
            //throw new InvalidOperationException("Workflow start state was not found.");
            throw new BusinessRuleException(
                "WorkflowStateNotFound",
                "Workflow start state was not found.");
        }

        if (!Enum.TryParse<TicketPriority>(
                request.Priority,
                ignoreCase: true,
                out var priority))
        {
            throw new InvalidOperationException("Invalid ticket priority.");
        }

        var number = await _numberGenerator.GenerateAsync(tenantId, cancellationToken);

        var ticket = new Ticket
        {
            TenantId = tenantId,
            OrganizationUnitId = request.OrganizationUnitId,
            TicketTypeId = ticketType.Id,
            WorkflowStateId = startState.Id,
            Number = number,
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = priority,
            CreatedByUserId = "demo-user",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);

        await _db.SaveChangesAsync(cancellationToken);

        return new TicketDetailsDto
        {
            Id = ticket.Id,
            Number = ticket.Number,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority.ToString(),
            TicketType = ticketType.Name,
            WorkflowState = startState.Name,
            CreatedAtUtc = ticket.CreatedAtUtc
        };
    }

    public async Task<TicketDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        return await _db.Tickets
            .AsNoTracking()
            .Where(x => x.Id == id
                        && x.TenantId == tenantId
                        && !x.IsDeleted)
            .Select(x => new TicketDetailsDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Description = x.Description,
                Priority = x.Priority.ToString(),
                TicketType = x.TicketType.Name,
                WorkflowState = x.WorkflowState.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketListItemDto>> SearchAsync(
        TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = _db.Tickets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText.Trim();

            query = query.Where(x =>
                x.Number.Contains(search) ||
                x.Title.Contains(search));
        }

        if (request.TicketTypeId is not null)
        {
            query = query.Where(x => x.TicketTypeId == request.TicketTypeId);
        }

        if (request.WorkflowStateId is not null)
        {
            query = query.Where(x => x.WorkflowStateId == request.WorkflowStateId);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            if (Enum.TryParse<TicketPriority>(
                    request.Priority,
                    ignoreCase: true,
                    out var priority))
            {
                query = query.Where(x => x.Priority == priority);
            }
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TicketListItemDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Priority = x.Priority.ToString(),
                TicketType = x.TicketType.Name,
                WorkflowState = x.WorkflowState.Name,
                AssignedToUserId = x.AssignedToUserId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
public async Task<TicketDetailsDto> ChangeStateAsync(
    int ticketId,
    ChangeTicketStateRequest request,
    CancellationToken cancellationToken)
{
    var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

    var ticket = await _db.Tickets
        .Include(x => x.TicketType)
        .Include(x => x.WorkflowState)
        .SingleOrDefaultAsync(
            x => x.Id == ticketId
                 && x.TenantId == tenantId
                 && !x.IsDeleted,
            cancellationToken);

    if (ticket is null)
    {
        throw new BusinessRuleException(
            "ticket_not_found",
            "Ticket was not found.");
    }

    var transition = await _db.WorkflowTransitions
        .Include(x => x.ToState)
        .SingleOrDefaultAsync(
            x => x.WorkflowDefinitionId == ticket.WorkflowState.WorkflowDefinitionId
                 && x.FromStateId == ticket.WorkflowStateId
                 && x.ToStateId == request.ToStateId
                 && x.IsActive,
            cancellationToken);

    if (transition is null)
    {
        throw new BusinessRuleException(
            "workflow_transition_not_allowed",
            "Workflow transition is not allowed.");
    }

    if (transition.RequiresComment && string.IsNullOrWhiteSpace(request.Comment))
    {
        throw new BusinessRuleException(
            "workflow_transition_comment_required",
            "Comment is required for this transition.");
    }

    var now = DateTime.UtcNow;
    var userId = "demo-user";

    var oldStateName = ticket.WorkflowState.Name;
    var newStateName = transition.ToState.Name;

    ticket.WorkflowStateId = transition.ToStateId;
    ticket.UpdatedAtUtc = now;
    ticket.UpdatedByUserId = userId;

    if (transition.ToState.IsFinal)
    {
        ticket.ClosedAtUtc = now;
    }

    _db.TicketHistoryEntries.Add(new TicketHistoryEntry
    {
        TicketId = ticket.Id,
        EventType = "StateChanged",
        OldValue = oldStateName,
        NewValue = newStateName,
        Comment = request.Comment,
        CreatedAtUtc = now,
        CreatedByUserId = userId
    });

    await _db.SaveChangesAsync(cancellationToken);

    return new TicketDetailsDto
    {
        Id = ticket.Id,
        Number = ticket.Number,
        Title = ticket.Title,
        Description = ticket.Description,
        Priority = ticket.Priority.ToString(),
        TicketType = ticket.TicketType.Name,
        WorkflowState = newStateName,
        CreatedAtUtc = ticket.CreatedAtUtc
    };
}
public async Task AddCommentAsync(
    int ticketId,
    AddCommentRequest request,
    CancellationToken cancellationToken)
{
    var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

    var ticket = await _db.Tickets
        .SingleOrDefaultAsync(
            x => x.Id == ticketId
                 && x.TenantId == tenantId
                 && !x.IsDeleted,
            cancellationToken);

    if (ticket is null)
    {
        throw new BusinessRuleException(
            "ticket_not_found",
            "Ticket was not found.");
    }

    if (string.IsNullOrWhiteSpace(request.Body))
    {
        throw new BusinessRuleException(
            "comment_body_required",
            "Comment body is required.");
    }

    var now = DateTime.UtcNow;
    var userId = "demo-user";
    var body = request.Body.Trim();

    _db.TicketComments.Add(new TicketComment
    {
        TicketId = ticket.Id,
        Body = body,
        IsInternal = request.IsInternal,
        CreatedAtUtc = now,
        CreatedByUserId = userId
    });

    _db.TicketHistoryEntries.Add(new TicketHistoryEntry
    {
        TicketId = ticket.Id,
        EventType = "CommentAdded",
        Comment = body,
        CreatedAtUtc = now,
        CreatedByUserId = userId
    });

    await _db.SaveChangesAsync(cancellationToken);
}
public async Task<IReadOnlyList<TicketHistoryItemDto>> GetHistoryAsync(
    int ticketId,
    CancellationToken cancellationToken)
{
    var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

    var ticketExists = await _db.Tickets
        .AnyAsync(
            x => x.Id == ticketId
                 && x.TenantId == tenantId
                 && !x.IsDeleted,
            cancellationToken);

    if (!ticketExists)
    {
        throw new BusinessRuleException(
            "ticket_not_found",
            "Ticket was not found.");
    }

    return await _db.TicketHistoryEntries
        .AsNoTracking()
        .Where(x => x.TicketId == ticketId)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new TicketHistoryItemDto
        {
            Id = x.Id,
            EventType = x.EventType,
            OldValue = x.OldValue,
            NewValue = x.NewValue,
            Comment = x.Comment,
            CreatedByUserId = x.CreatedByUserId,
            CreatedAtUtc = x.CreatedAtUtc
        })
        .ToListAsync(cancellationToken);
}
}