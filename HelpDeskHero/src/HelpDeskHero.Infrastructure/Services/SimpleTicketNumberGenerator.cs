using HelpDeskHero.Application.Tickets;
using HelpDeskHero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Infrastructure.Services;

public sealed class SimpleTicketNumberGenerator : ITicketNumberGenerator
{
    private readonly AppDbContext _db;

    public SimpleTicketNumberGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var count = await _db.Tickets
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        return $"HDH-{DateTime.UtcNow:yyyy}-{count + 1:00000}";
    }
}