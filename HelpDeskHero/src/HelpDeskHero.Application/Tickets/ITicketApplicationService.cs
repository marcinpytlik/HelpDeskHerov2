using HelpDeskHero.Application.Tickets.Dtos;

namespace HelpDeskHero.Application.Tickets;

public interface ITicketApplicationService
{
    Task<TicketDetailsDto> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken);

    Task<TicketDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketListItemDto>> SearchAsync(
        TicketSearchRequest request,
        CancellationToken cancellationToken);

    Task<TicketDetailsDto> ChangeStateAsync(
        int ticketId,
        ChangeTicketStateRequest request,
        CancellationToken cancellationToken);

    Task AddCommentAsync(
        int ticketId,
        AddCommentRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketHistoryItemDto>> GetHistoryAsync(
        int ticketId,
        CancellationToken cancellationToken);
}