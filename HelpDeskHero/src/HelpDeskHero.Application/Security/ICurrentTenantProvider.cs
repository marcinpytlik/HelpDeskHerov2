namespace HelpDeskHero.Application.Security;

public interface ICurrentTenantProvider
{
    Task<int> GetCurrentTenantIdAsync(CancellationToken cancellationToken);
}
