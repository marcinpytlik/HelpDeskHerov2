using HelpDeskHero.Application.Security;
using HelpDeskHero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Identity;

public sealed class DemoCurrentTenantProvider : ICurrentTenantProvider
{
    private readonly AppDbContext _db;

    public DemoCurrentTenantProvider(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCurrentTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        return tenant.Id;
    }
}
