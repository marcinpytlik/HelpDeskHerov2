using HelpDeskHero.Application.Security;

namespace HelpDeskHero.Infrastructure.Security;

public sealed class DemoCurrentUserProvider : ICurrentUserProvider
{
    public string GetCurrentUserId()
    {
        return "demo-user";
    }
}