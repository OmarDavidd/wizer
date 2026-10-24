using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Revokes <paramref name="tokenId"/> and links it to its replacement; <c>false</c> if it was already revoked.</summary>
    Task<bool> MarkRotatedAsync(Guid tokenId, Guid replacedById, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary><c>false</c> if it was already revoked.</summary>
    Task<bool> RevokeAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Signs the user out everywhere (password reset).</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes every live token of the family (reuse detected, or the user can no longer sign in).</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);
}
