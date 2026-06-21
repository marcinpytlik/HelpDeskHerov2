namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class ChangeTicketStateRequest
{
    public int ToStateId { get; set; }

    public string? Comment { get; set; }
}