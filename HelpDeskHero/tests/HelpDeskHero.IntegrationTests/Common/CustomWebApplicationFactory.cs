/*using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace HelpDeskHero.IntegrationTests.Common;
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program> {
     protected override void ConfigureWebHost(IWebHostBuilder builder) 
     { builder.UseEnvironment("Development"); } 
}*/

/*using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace HelpDeskHero.IntegrationTests.Common;
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program> {
     protected override void ConfigureWebHost(IWebHostBuilder builder) 
     { builder.UseEnvironment("Testing"); } 
}*/
using HelpDeskHero.Infrastructure.Persistence;
using HelpDeskHero.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskHero.IntegrationTests.Common;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        await db.Database.MigrateAsync();

        await seeder.SeedAsync(CancellationToken.None);

        await ClearTestDataAsync(db);
    }

    public async Task ClearTestDataAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ClearTestDataAsync(db);
    }

    private static async Task ClearTestDataAsync(AppDbContext db)
    {
        await db.TicketHistoryEntries.ExecuteDeleteAsync();
        await db.TicketComments.ExecuteDeleteAsync();
        await db.Tickets.ExecuteDeleteAsync();
    }
}