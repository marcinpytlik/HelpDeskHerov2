namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class TicketSearchRequest
{
    public string? SearchText { get; set; }

    public int? TicketTypeId { get; set; }

    public int? WorkflowStateId { get; set; }

    public string? Priority { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}