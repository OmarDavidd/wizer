using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Infrastructure.Persistence;

/// <summary>
/// The request's <see cref="AppDbContext"/> as a unit of work. Disposing the transaction without
/// <see cref="CommitAsync"/> rolls it back.
/// </summary>
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken cancellationToken) =>
        await db.Database.BeginTransactionAsync(cancellationToken);

    public Task CommitAsync(CancellationToken cancellationToken) =>
        db.Database.CurrentTransaction?.CommitAsync(cancellationToken)
        ?? throw new InvalidOperationException("There is no transaction to commit.");

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
