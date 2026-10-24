using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Time;

namespace Wizer.UnitTests.Infrastructure.Authentication;

public class AccessTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_WritesSubjectRoleInstitutionAndUniqueJti()
    {
        var issuer = CreateIssuer();
        var userId = Guid.NewGuid();
        var institutionId = Guid.NewGuid();

        var first = new JsonWebToken(issuer.Issue(userId, PlatformRole.Teacher, institutionId).Value);
        var second = new JsonWebToken(issuer.Issue(userId, PlatformRole.Teacher, institutionId).Value);

        Assert.Equal(userId.ToString(), first.GetClaim(WizerClaimTypes.Subject).Value);
        Assert.Equal("teacher", first.GetClaim(WizerClaimTypes.Role).Value);
        Assert.Equal(institutionId.ToString(), first.GetClaim(WizerClaimTypes.Institution).Value);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("HS256", first.Alg);
    }

    [Theory]
    [InlineData(PlatformRole.Student, "student")]
    [InlineData(PlatformRole.Teacher, "teacher")]
    [InlineData(PlatformRole.Admin, "admin")]
    public void Issue_RoleClaim_UsesTheExactNamesReadByCurrentUser(PlatformRole role, string expected)
    {
        var token = new JsonWebToken(CreateIssuer().Issue(Guid.NewGuid(), role, Guid.NewGuid()).Value);

        Assert.Equal(expected, token.GetClaim(WizerClaimTypes.Role).Value);
    }

    [Fact]
    public void Issue_ExpiresAfterConfiguredMinutes()
    {
        var accessToken = CreateIssuer(accessMinutes: 15).Issue(Guid.NewGuid(), PlatformRole.Student, Guid.NewGuid());

        Assert.Equal(Now.AddMinutes(15), accessToken.ExpiresAt);
        Assert.Equal(Now.AddMinutes(15).UtcDateTime, new JsonWebToken(accessToken.Value).ValidTo);
    }

    private static AccessTokenIssuer CreateIssuer(int accessMinutes = 15)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            SigningKey = "a-signing-key-that-has-at-least-32-bytes",
            AccessTokenMinutes = accessMinutes,
        });
        var environment = Mock.Of<IHostEnvironment>(env => env.EnvironmentName == Environments.Production);
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        return new AccessTokenIssuer(new JwtSigningKey(options, environment), options, clock);
    }
}
