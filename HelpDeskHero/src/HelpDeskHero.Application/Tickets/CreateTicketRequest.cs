namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class CreateTicketRequest
{
    public int TicketTypeId { get; set; }

    public int? OrganizationUnitId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Priority { get; set; } = "Normal";
}
