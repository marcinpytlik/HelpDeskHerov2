using HelpDeskHero.Domain.Common;

namespace HelpDeskHero.Domain.Entities;

public sealed class TicketComment : AuditableEntity
{
    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = default!;

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}