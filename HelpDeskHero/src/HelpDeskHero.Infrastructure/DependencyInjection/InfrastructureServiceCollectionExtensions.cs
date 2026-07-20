using HelpDeskHero.Application.Common;
using HelpDeskHero.Application.Security;
using HelpDeskHero.Application.Tickets;
using HelpDeskHero.Infrastructure.Identity;
using HelpDeskHero.Infrastructure.Persistence;
using HelpDeskHero.Infrastructure.Persistence.Interceptors;
using HelpDeskHero.Infrastructure.Persistence.Seeding;
using HelpDeskHero.Infrastructure.Security;
using HelpDeskHero.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskHero.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");
        }

        services.AddScoped<ICurrentTenantProvider, DemoCurrentTenantProvider>();
        services.AddScoped<ICurrentUserProvider, DemoCurrentUserProvider>();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var auditableInterceptor =
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>();

            options.UseSqlServer(connectionString);

            options.AddInterceptors(auditableInterceptor);
        });

        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        services.AddScoped<TicketBusinessValidator>();
        services.AddScoped<ITicketNumberGenerator, SimpleTicketNumberGenerator>();
        services.AddScoped<ITicketApplicationService, TicketApplicationService>();

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        services.AddScoped<ISeedStep, TenantSeedStep>();
        services.AddScoped<ISeedStep, OrganizationUnitSeedStep>();
        services.AddScoped<ISeedStep, TicketTypeSeedStep>();
        services.AddScoped<ISeedStep, WorkflowSeedStep>();

        return services;
    }
}