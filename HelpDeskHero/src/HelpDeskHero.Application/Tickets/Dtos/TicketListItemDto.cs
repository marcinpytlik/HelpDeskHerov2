namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class TicketListItemDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string TicketType { get; set; } = string.Empty;

    public string WorkflowState { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}