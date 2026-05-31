namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class TicketDetailsDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Priority { get; set; } = string.Empty;

    public string TicketType { get; set; } = string.Empty;

    public string WorkflowState { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}