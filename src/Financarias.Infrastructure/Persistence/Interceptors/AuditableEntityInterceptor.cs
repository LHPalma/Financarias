using Financarias.Application.Common.Security;
using Financarias.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Financarias.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider
) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                // não faz nada ¯\_(ツ)_/¯
                continue;
            }

            var isNew = entry.State == EntityState.Added;

            if (isNew)
            {
                entry.Property(nameof(IHasTimestamps.CreatedAt)).CurrentValue = now;
            }

            entry.Property(nameof(IHasTimestamps.UpdatedAt)).CurrentValue = now;

            if (entry.Entity is not IAuditable)
            {
                // não faz nada ¯\_(ツ)_/¯
                continue;
            }

            if (isNew)
            {
                entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = userId;
            }

            entry.Property(nameof(IAuditable.UpdatedBy)).CurrentValue = userId;
        }
    }
}
