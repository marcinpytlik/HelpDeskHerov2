namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class TicketHistoryItemDto
{
    public int Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Comment { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}