using HelpDeskHero.Infrastructure.Persistence;
using HelpDeskHero.Infrastructure.Persistence.Seeding;
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

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        services.AddScoped<ISeedStep, TenantSeedStep>();
        services.AddScoped<ISeedStep, OrganizationUnitSeedStep>();
        services.AddScoped<ISeedStep, TicketTypeSeedStep>();
        services.AddScoped<ISeedStep, WorkflowSeedStep>();

        return services;
    }
}