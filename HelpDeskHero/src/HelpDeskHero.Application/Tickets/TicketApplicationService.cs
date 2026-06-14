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
}