using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

    DbSet<TicketComment> TicketComments { get; }

    DbSet<TicketHistoryEntry> TicketHistoryEntries { get; }
EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
    where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}