using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wizer.Api.Shared.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Infrastructure.Persistence;

/// <summary>
/// Fills <c>created_at</c> and <c>updated_at</c> from <see cref="IClock"/>. A <c>created_at</c> already set
/// is kept (the seed writes past dates). Authorship columns (<c>created_by_id</c>, <c>uploaded_by_id</c>,
/// <c>graded_by_id</c>) are set by the Service: background workers and the seed have no current user.
/// </summary>
public sealed class AuditInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is IHasCreatedAt created && created.CreatedAt == default)
                    {
                        created.CreatedAt = now;
                    }

                    if (entry.Entity is IHasUpdatedAt added && added.UpdatedAt == default)
                    {
                        added.UpdatedAt = (entry.Entity as IHasCreatedAt)?.CreatedAt ?? now;
                    }

                    break;
                case EntityState.Modified when entry.Entity is IHasUpdatedAt updated:
                    updated.UpdatedAt = now;
                    break;
            }
        }
    }
}
