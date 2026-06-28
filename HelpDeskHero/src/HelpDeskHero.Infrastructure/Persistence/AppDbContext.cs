using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HelpDeskHero.Application.Common;

namespace HelpDeskHero.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
public DbSet<TicketComment> TicketComments => Set<TicketComment>();

public DbSet<TicketHistoryEntry> TicketHistoryEntries => Set<TicketHistoryEntry>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}