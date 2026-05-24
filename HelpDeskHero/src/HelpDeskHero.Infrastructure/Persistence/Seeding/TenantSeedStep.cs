using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class TenantSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 10;

    public TenantSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var exists = await _db.Tenants
            .AnyAsync(x => x.Code == "DEMO", cancellationToken);

        if (exists)
        {
            return;
        }

        var tenant = new Tenant
        {
            Code = "DEMO",
            Name = "Demo Tenant",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = "system-seed"
        };

        _db.Tenants.Add(tenant);

        await _db.SaveChangesAsync(cancellationToken);
    }
}