namespace HelpDeskHero.Application.Tickets.Dtos;

public sealed class AddCommentRequest
{
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}