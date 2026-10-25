using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Repositories;

public sealed class InvitationTokenRepository(AppDbContext db) : IInvitationTokenRepository
{
    public void Add(InvitationToken token) => db.Add(token);

    public Task<InvitationToken?> FindUsableByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        Pending().AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash && token.ExpiresAt > now, cancellationToken);

    public Task RevokePendingAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Pending().Where(token => token.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken);

    public async Task<bool> ConsumeAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await Pending().Where(token => token.Id == tokenId && token.ExpiresAt > now)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.UsedAt, now), cancellationToken) == 1;

    private IQueryable<InvitationToken> Pending() =>
        db.Set<InvitationToken>().Where(token => token.UsedAt == null && token.RevokedAt == null);
}
