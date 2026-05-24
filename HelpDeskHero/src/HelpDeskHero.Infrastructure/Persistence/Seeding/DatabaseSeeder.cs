namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IEnumerable<ISeedStep> _steps;

    public DatabaseSeeder(IEnumerable<ISeedStep> steps)
    {
        _steps = steps;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var step in _steps.OrderBy(x => x.Order))
        {
            await step.ExecuteAsync(cancellationToken);
        }
    }
}