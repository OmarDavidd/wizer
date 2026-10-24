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
    public IssuedSession Issue(User user, bool rememberMe) => Add(user, familyId: ids.NewId(), rememberMe);

    public IssuedSession Rotate(User user, RefreshToken previous) => Add(user, previous.FamilyId, previous.IsPersistent);

    private IssuedSession Add(User user, Guid familyId, bool persistent)
    {
        var token = SecretHash.NewToken();
        var days = persistent ? options.Value.RefreshTokenRememberDays : options.Value.RefreshTokenDays;
        var expiresAt = clock.UtcNow.AddDays(days);
        var id = ids.NewId();

        refreshTokens.Add(new RefreshToken
        {
            Id = id,
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = SecretHash.Of(token),
            IsPersistent = persistent,
            ExpiresAt = expiresAt,
        });

        return new IssuedSession(
            accessTokens.Issue(user.Id, user.PlatformRole, user.InstitutionId),
            token,
            id,
            expiresAt);
    }
}
