using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class TicketHistoryEntry : AuditableEntity
{
    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = default!;

    public string EventType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Comment { get; set; }
}