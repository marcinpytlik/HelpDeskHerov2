using HelpDeskHero.Application.Tickets.Dtos;

namespace HelpDeskHero.Application.Tickets;

public interface ITicketApplicationService
{
    Task<TicketDetailsDto> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken);
}
