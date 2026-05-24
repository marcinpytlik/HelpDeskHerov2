using HelpDeskHero.Infrastructure.Persistence.Seeding;

namespace HelpDeskHero.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        await seeder.SeedAsync(CancellationToken.None);
    }
}