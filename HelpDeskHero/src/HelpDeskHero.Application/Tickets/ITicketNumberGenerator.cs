namespace HelpDeskHero.Application.Tickets;

public interface ITicketNumberGenerator
{
    Task<string> GenerateAsync(
        int tenantId,
        CancellationToken cancellationToken);
}