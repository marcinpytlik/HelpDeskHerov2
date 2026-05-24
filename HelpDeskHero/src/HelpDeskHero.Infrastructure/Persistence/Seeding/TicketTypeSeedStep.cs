using HelpDeskHero.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Persistence.Seeding;

public sealed class TicketTypeSeedStep : ISeedStep
{
    private readonly AppDbContext _db;

    public int Order => 30;

    public TicketTypeSeedStep(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleAsync(x => x.Code == "DEMO", cancellationToken);

        var existingCodes = await _db.TicketTypes
            .Where(x => x.TenantId == tenant.Id)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var items = new List<TicketType>();

        if (!existingCodes.Contains("INCIDENT"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "INCIDENT",
                Name = "Incident",
                Description = "Unexpected interruption or degradation of service.",
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = "system-seed"
            });
        }

        if (!existingCodes.Contains("SERVICE_REQUEST"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "SERVICE_REQUEST",
                Name = "Service Request",
                Description = "Standard user request.",
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = "system-seed"
            });
        }

        if (!existingCodes.Contains("ACCESS_REQUEST"))
        {
            items.Add(new TicketType
            {
                TenantId = tenant.Id,
                Code = "ACCESS_REQUEST",
                Name = "Access Request",
                Description = "Request for access to a system or resource.",
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = "system-seed"
            });
        }

        if (items.Count == 0)
        {
            return;
        }

        _db.TicketTypes.AddRange(items);

        await _db.SaveChangesAsync(cancellationToken);
    }
}