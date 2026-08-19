using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ReactionLab.Infrastructure.Persistence.Interceptors;

internal sealed class AuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
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

    public void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Metadata.FindProperty(PersistenceColumns.UpdatedAt) is null)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(PersistenceColumns.CreatedAt).CurrentValue = now;
                    entry.Property(PersistenceColumns.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(PersistenceColumns.UpdatedAt).CurrentValue = now;
                    break;
                default:
                    break;
            }
        }
    }
}
