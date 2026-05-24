namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public interface ISeedStep
{
    int Order { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}