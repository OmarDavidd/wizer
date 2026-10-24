namespace Wizer.Api.Shared.Persistence;

/// <summary>One transaction for a multi-step business operation (.ia/backend.md §Transacciones).</summary>
public interface IUnitOfWork
{
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
