using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wizer.Api.Shared.Auth;

namespace Wizer.UnitTests.Shared.Auth;

public class HttpContextCurrentUserTests
{
    private static HttpContextCurrentUser CreateSut(params Claim[] claims)
    {
        var identity = claims.Length == 0 ? new ClaimsIdentity() : new ClaimsIdentity(claims, "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void Properties_AuthenticatedUser_ReadFromClaims()
    {
        var userId = Guid.CreateVersion7();
        var institutionId = Guid.CreateVersion7();

        var sut = CreateSut(
            new Claim(WizerClaimTypes.Subject, userId.ToString()),
            new Claim(WizerClaimTypes.Role, "teacher"),
            new Claim(WizerClaimTypes.Institution, institutionId.ToString()));

        Assert.Equal(userId, sut.Id);
        Assert.Equal(PlatformRole.Teacher, sut.Role);
        Assert.Equal(institutionId, sut.InstitutionId);
    }

    [Fact]
    public void Id_AnonymousUser_ThrowsInvalidOperation()
    {
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(() => sut.Id);
    }

    [Fact]
    public void Role_UnknownRoleClaim_ThrowsInvalidOperation()
    {
        var sut = CreateSut(
            new Claim(WizerClaimTypes.Subject, Guid.CreateVersion7().ToString()),
            new Claim(WizerClaimTypes.Role, "superuser"));

        Assert.Throws<InvalidOperationException>(() => sut.Role);
    }
}
