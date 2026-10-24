using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => db.Add(token);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>().AsNoTracking().SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> MarkRotatedAsync(Guid tokenId, Guid replacedById, DateTimeOffset now, CancellationToken cancellationToken) =>
        await Live().Where(token => token.Id == tokenId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.ReplacedById, replacedById), cancellationToken) == 1;

    public async Task<bool> RevokeAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await Live().Where(token => token.Id == tokenId)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken) == 1;

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Live().Where(token => token.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken);

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Live().Where(token => token.FamilyId == familyId)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken);

    private IQueryable<RefreshToken> Live() => db.Set<RefreshToken>().Where(token => token.RevokedAt == null);
}
