using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Infrastructure.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IAccessTokenIssuer
{
    AccessToken Issue(Guid userId, PlatformRole role, Guid institutionId);
}

/// <summary>
/// Signs the short-lived access JWT: <c>sub</c>, <c>role</c>, <c>inst</c> and a unique <c>jti</c>.
/// Class membership is never in the token; services resolve it per request.
/// </summary>
public sealed class AccessTokenIssuer(JwtSigningKey signingKey, IOptions<JwtOptions> options, IClock clock)
    : IAccessTokenIssuer
{
    public const string Issuer = "wizer";
    public const string Audience = "wizer";

    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken Issue(Guid userId, PlatformRole role, Guid institutionId)
    {
        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(options.Value.AccessTokenMinutes);

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(WizerClaimTypes.Subject, userId.ToString()),
                new Claim(WizerClaimTypes.Role, role.ToClaimValue()),
                new Claim(WizerClaimTypes.Institution, institutionId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey.Key, SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
