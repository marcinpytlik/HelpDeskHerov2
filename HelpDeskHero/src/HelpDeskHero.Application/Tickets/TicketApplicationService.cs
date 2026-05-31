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

    public TicketApplicationService(
        IAppDbContext db,
        ICurrentTenantProvider tenantProvider,
        ITicketNumberGenerator numberGenerator)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _numberGenerator = numberGenerator;
    }

    public async Task<TicketDetailsDto> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Ticket title is required.");
        }

        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var ticketType = await _db.TicketTypes
            .SingleOrDefaultAsync(
                x => x.Id == request.TicketTypeId
                     && x.TenantId == tenantId
                     && x.IsActive,
                cancellationToken);

        if (ticketType is null)
        {
            throw new InvalidOperationException("Ticket type was not found.");
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
                throw new InvalidOperationException("Organization unit was not found.");
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
            throw new InvalidOperationException("Default workflow was not found.");
        }

        var startState = workflow.States.SingleOrDefault(x => x.IsStart);

        if (startState is null)
        {
            throw new InvalidOperationException("Workflow start state was not found.");
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
}