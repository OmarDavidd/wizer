using Microsoft.Extensions.Options;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Classes;

public sealed class SessionIssuer(
    IRefreshTokenRepository refreshTokens,
    IAccessTokenIssuer accessTokens,
    IOptions<JwtOptions> options,
    IIdGenerator ids,
    IClock clock) : ISessionIssuer
{
    public IssuedSession Issue(User user, bool rememberMe)
    {
        var token = SecretHash.NewToken();
        var days = rememberMe ? options.Value.RefreshTokenRememberDays : options.Value.RefreshTokenDays;
        var expiresAt = clock.UtcNow.AddDays(days);

        refreshTokens.Add(new RefreshToken
        {
            Id = ids.NewId(),
            UserId = user.Id,
            FamilyId = ids.NewId(),
            TokenHash = SecretHash.Of(token),
            IsPersistent = rememberMe,
            ExpiresAt = expiresAt,
        });

        return new IssuedSession(
            accessTokens.Issue(user.Id, user.PlatformRole, user.InstitutionId),
            token,
            expiresAt);
    }
}
