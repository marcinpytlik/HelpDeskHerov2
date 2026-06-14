namespace HelpDeskHero.Shared.Api;

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}