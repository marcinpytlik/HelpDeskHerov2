using HelpDeskHero.Domain.Entities;
using HelpDeskHero.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class OrganizationUnitSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 20;

    public OrganizationUnitSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var exists = await _db.OrganizationUnits
            .AnyAsync(
                x => x.TenantId == tenant.Id && x.Code == "IT",
                cancellationToken);

        if (exists)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var it = new OrganizationUnit
        {
            TenantId = tenant.Id,
            Code = "IT",
            Name = "IT Department",
            Type = OrganizationUnitType.Department,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var helpdesk = new OrganizationUnit
        {
            TenantId = tenant.Id,
            ParentOrganizationUnit = it,
            Code = "HELPDESK",
            Name = "Helpdesk Team",
            Type = OrganizationUnitType.Team,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        var infrastructure = new OrganizationUnit
        {
            TenantId = tenant.Id,
            ParentOrganizationUnit = it,
            Code = "INFRA",
            Name = "Infrastructure Team",
            Type = OrganizationUnitType.Team,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = "system-seed"
        };

        _db.OrganizationUnits.AddRange(it, helpdesk, infrastructure);

        await _db.SaveChangesAsync(cancellationToken);
    }
}