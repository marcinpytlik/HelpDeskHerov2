using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
namespace HelpDeskHero.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var basePath = Path.Combine(
            currentDirectory,
            "src",
            "HelpDeskHero.Api");

        if (!Directory.Exists(basePath))
        {
            basePath = Path.GetFullPath(
                Path.Combine(
                    currentDirectory,
                    "..",
                    "HelpDeskHero.Api"));
        }

        if (!Directory.Exists(basePath))
        {
            throw new DirectoryNotFoundException(
                $"Nie znaleziono katalogu projektu API dla konfiguracji design-time. Sprawdzona ścieżka: {basePath}");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("MigrationConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Brak connection stringa 'MigrationConnection' albo 'DefaultConnection'.");
        }
var csb = new SqlConnectionStringBuilder(connectionString);

var usedConnectionName =
    configuration.GetConnectionString("MigrationConnection") is not null
        ? "MigrationConnection"
        : "DefaultConnection";

Console.WriteLine("==============================================");
Console.WriteLine("EF Core design-time DbContext factory");
Console.WriteLine($"Connection name : {usedConnectionName}");
Console.WriteLine($"Server          : {csb.DataSource}");
Console.WriteLine($"Database        : {csb.InitialCatalog}");
Console.WriteLine($"User Id         : {csb.UserID}");
Console.WriteLine("==============================================");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
