using HelpDeskHero.Application.Security;
using HelpDeskHero.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HelpDeskHero.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserProvider _currentUserProvider;

    public AuditableEntitySaveChangesInterceptor(
        ICurrentUserProvider currentUserProvider)
    {
        _currentUserProvider = currentUserProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var userId = _currentUserProvider.GetCurrentUserId();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                SetCreated(entry, now, userId);
            }

            if (entry.State == EntityState.Modified)
            {
                SetUpdated(entry, now, userId);
            }
        }
    }

    private static void SetCreated(
        EntityEntry<AuditableEntity> entry,
        DateTime now,
        string userId)
    {
        entry.Entity.CreatedAtUtc = now;
        entry.Entity.CreatedByUserId = userId;
    }

    private static void SetUpdated(
        EntityEntry<AuditableEntity> entry,
        DateTime now,
        string userId)
    {
        entry.Entity.UpdatedAtUtc = now;
        entry.Entity.UpdatedByUserId = userId;
    }
}
