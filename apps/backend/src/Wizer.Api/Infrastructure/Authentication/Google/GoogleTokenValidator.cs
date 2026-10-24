using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Wizer.Api.Infrastructure.Authentication.Google;

/// <summary>
/// Validates a Google Identity Services credential: RS256 signature against Google's published keys
/// (cached and refreshed by the configuration manager), <c>iss</c>, <c>aud</c> = our client id, expiry and
/// <c>email_verified = true</c> (.ia/authentication.md §Login con Google).
/// </summary>
public sealed class GoogleTokenValidator(
    IOptions<GoogleOptions> options,
    IConfigurationManager<OpenIdConnectConfiguration> googleConfiguration,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    public const string DiscoveryUrl = "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly JsonWebTokenHandler Handler = new();

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = options.Value.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogWarning("Google sign-in rejected: Google:ClientId is not configured");
            return null;
        }

        var result = await ValidateWithGoogleKeysAsync(idToken, clientId, cancellationToken);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Google rotated its keys since they were cached: fetch them again and retry once.
            googleConfiguration.RequestRefresh();
            result = await ValidateWithGoogleKeysAsync(idToken, clientId, cancellationToken);
        }

        if (result.IsValid is false
            || result.Claims.TryGetValue("sub", out var subject) is false
            || result.Claims.TryGetValue("email", out var email) is false
            || result.Claims.TryGetValue("email_verified", out var verified) is false
            || IsTrue(verified) is false)
        {
            return null;
        }

        var name = result.Claims.TryGetValue("name", out var value) ? value.ToString() : null;
        return new GoogleIdentity(subject.ToString()!, email.ToString()!, string.IsNullOrWhiteSpace(name) ? null : name.Trim());
    }

    private async Task<TokenValidationResult> ValidateWithGoogleKeysAsync(
        string idToken,
        string clientId,
        CancellationToken cancellationToken)
    {
        var configuration = await googleConfiguration.GetConfigurationAsync(cancellationToken);
        return await Handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            ValidAudience = clientId,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
        });
    }

    // Google sends a JSON boolean; tolerate the string form too.
    private static bool IsTrue(object value) =>
        value is true || (value is string text && bool.TryParse(text, out var parsed) && parsed);
}
