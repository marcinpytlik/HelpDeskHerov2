namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class UpdateTicketRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Priority { get; set; } = string.Empty;

    public string RowVersion { get; set; } = string.Empty;
}