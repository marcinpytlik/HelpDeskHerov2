namespace HelpDeskHero.Application.Common;

public sealed class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}