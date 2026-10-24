using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface ISessionService
{
    /// <summary>Signs in and writes the session cookies.</summary>
    Task<MeResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates the refresh token and writes new cookies. <c>null</c> when the token cannot be used; the
    /// cookies are then cleared (the caller answers 401 without throwing, so the cleared cookies survive).
    /// </summary>
    Task<MeResponse?> RefreshAsync(string? refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the refresh token, if any, and clears the cookies.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);

    /// <summary>The current user, or <c>null</c> if the account no longer exists or is inactive.</summary>
    Task<MeResponse?> GetMeAsync(CancellationToken cancellationToken);
}
