using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IInvitationTokenRepository
{
    void Add(InvitationToken token);

    /// <summary>The token if it is unused, unrevoked and unexpired at <paramref name="now"/>.</summary>
    Task<InvitationToken?> FindUsableByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes the user's pending invitations (a resend invalidates the previous link).</summary>
    Task RevokePendingAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Marks it used; <c>false</c> if another request used or revoked it first.</summary>
    Task<bool> ConsumeAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken);
}
