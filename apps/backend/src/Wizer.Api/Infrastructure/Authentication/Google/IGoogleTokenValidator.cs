namespace Wizer.Api.Infrastructure.Authentication.Google;

public interface IGoogleTokenValidator
{
    /// <summary>The identity in a valid Google ID token with a verified email; <c>null</c> otherwise.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
