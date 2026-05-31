using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Application.Common;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<OrganizationUnit> OrganizationUnits { get; }
    DbSet<TicketType> TicketTypes { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowState> WorkflowStates { get; }
    DbSet<WorkflowTransition> WorkflowTransitions { get; }
    DbSet<Ticket> Tickets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}