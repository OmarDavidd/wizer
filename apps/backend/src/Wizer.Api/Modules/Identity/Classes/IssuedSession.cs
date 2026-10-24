using Wizer.Api.Infrastructure.Authentication;

namespace Wizer.Api.Modules.Identity.Classes;

/// <summary>The values for the session cookies; <see cref="RefreshToken"/> is the only copy of the plain token.</summary>
public sealed record IssuedSession(
    AccessToken AccessToken,
    string RefreshToken,
    Guid RefreshTokenId,
    DateTimeOffset RefreshExpiresAt);
