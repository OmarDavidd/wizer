using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Repositories;

public sealed class VerificationCodeRepository(AppDbContext db) : IVerificationCodeRepository
{
    private IQueryable<VerificationCode> Pending(Guid userId, VerificationPurpose purpose) =>
        db.Set<VerificationCode>().Where(code =>
            code.UserId == userId && code.Purpose == purpose && code.ConsumedAt == null);

    public Task<DateTimeOffset?> FindPendingCreatedAtAsync(
        Guid userId,
        VerificationPurpose purpose,
        CancellationToken cancellationToken) =>
        Pending(userId, purpose).Select(code => (DateTimeOffset?)code.CreatedAt).SingleOrDefaultAsync(cancellationToken);

    public async Task ReplaceAsync(VerificationCode code, CancellationToken cancellationToken)
    {
        // The unique index allows one unconsumed code per user and purpose, so the old one goes first.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await Pending(code.UserId, code.Purpose).ExecuteDeleteAsync(cancellationToken);
        db.Add(code);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<VerificationCode?> ReserveAttemptAsync(
        Guid userId,
        VerificationPurpose purpose,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var reserved = await Pending(userId, purpose)
            .Where(code => code.ExpiresAt > now && code.Attempts < maxAttempts)
            .ExecuteUpdateAsync(set => set.SetProperty(code => code.Attempts, code => code.Attempts + 1), cancellationToken);

        return reserved == 0
            ? null
            : await Pending(userId, purpose).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ConsumeAsync(Guid codeId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Set<VerificationCode>()
            .Where(code => code.Id == codeId && code.ConsumedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(code => code.ConsumedAt, now), cancellationToken) == 1;
}
