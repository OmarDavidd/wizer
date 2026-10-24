using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Identity;

[Collection(PostgresCollection.Name)]
public sealed class SessionTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "contrasena123";

    private readonly WizerApiFactory _factory = new(postgres);
    private readonly SchemaBuilder _schema = new(postgres);
    private Campus _campus = null!;

    public async Task InitializeAsync()
    {
        await _schema.WithInstitutionAsync();
        _campus = await _schema.CampusAsync();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // --- Login (AUTH-02, AUTH-06) ---

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithSessionCookiesFor7Days()
    {
        var user = await UserAsync();

        var response = await LoginAsync(user.Email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(user.Email, body.RootElement.GetProperty("email").GetString());
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_access"));
        var token = await SingleRefreshTokenAsync(user.Id);
        Assert.False(token.IsPersistent);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(7), token.ExpiresAt);
    }

    [Fact]
    public async Task Login_RememberMe_KeepsTheRefreshTokenFor30Days()
    {
        var user = await UserAsync();

        var response = await LoginAsync(user.Email, Password, rememberMe: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await SingleRefreshTokenAsync(user.Id);
        Assert.True(token.IsPersistent);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(30), token.ExpiresAt);
    }

    [Fact]
    public async Task Login_EmailWithOtherCaseAndSpaces_Works()
    {
        var user = await UserAsync();

        var response = await LoginAsync($"  {user.Email.ToUpperInvariant()} ", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401InvalidCredentialsWithoutCookies()
    {
        var user = await UserAsync();

        var response = await LoginAsync(user.Email, "otraclave999");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        Assert.Empty(AuthHttp.SetCookies(response));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401InvalidCredentials()
    {
        var response = await LoginAsync($"{Guid.NewGuid():N}@test.local", Password);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_UnverifiedAccount_Returns403AndSendsCodeWithoutCookies()
    {
        var user = await UserAsync(verified: false);

        var response = await LoginAsync(user.Email, Password);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Forbidden, "EMAIL_NOT_VERIFIED");
        Assert.Empty(AuthHttp.SetCookies(response));
        await _factory.Emails.WaitForAsync(user.Email);
    }

    [Fact]
    public async Task Login_UnverifiedAccountWithWrongPassword_Returns401WithoutSendingCode()
    {
        var user = await UserAsync(verified: false);

        var response = await LoginAsync(user.Email, "otraclave999");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        await InMemoryEmailSender.SettleAsync();
        Assert.Empty(_factory.Emails.SentTo(user.Email));
    }

    [Fact]
    public async Task Login_InactiveAccount_Returns403AccountInactive()
    {
        var user = await UserAsync(active: false);

        var response = await LoginAsync(user.Email, Password);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Forbidden, "ACCOUNT_INACTIVE");
        Assert.Empty(AuthHttp.SetCookies(response));
    }

    // --- Lockout (AUTH-10) ---

    [Fact]
    public async Task Login_AfterFiveFailures_Returns429EvenWithTheCorrectPassword()
    {
        var user = await UserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(user.Email, "otraclave999");
        }

        var response = await LoginAsync(user.Email, Password);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.TooManyRequests, "TOO_MANY_LOGIN_ATTEMPTS");
        Assert.Equal("300", response.Headers.GetValues("Retry-After").Single());
    }

    [Fact]
    public async Task Login_LockoutCountsTheNormalizedEmail()
    {
        var user = await UserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(attempt % 2 == 0 ? user.Email.ToUpperInvariant() : $" {user.Email}", "otraclave999");
        }

        var response = await LoginAsync(user.Email, Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterTheLockoutWindow_AllowsTheCorrectPassword()
    {
        var user = await UserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(user.Email, "otraclave999");
        }

        _factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var response = await LoginAsync(user.Email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_IsAlsoLockedOut()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(email, "otraclave999");
        }

        var response = await LoginAsync(email, "otraclave999");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    // --- Me ---

    [Fact]
    public async Task Me_WithAccessCookie_ReturnsTheCurrentUser()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/auth/me", ("wz_access", AuthHttp.CookieValue(login, "wz_access")!));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("student", body.RootElement.GetProperty("role").GetString());
        Assert.Equal(_campus.Id, body.RootElement.GetProperty("campusId").GetGuid());
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/auth/me");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    // --- Refresh ---

    [Fact]
    public async Task Refresh_ValidToken_RotatesItAndIssuesNewCookies()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password, rememberMe: true);
        var first = AuthHttp.CookieValue(login, "wz_refresh")!;

        var response = await RefreshAsync(first);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var second = AuthHttp.CookieValue(response, "wz_refresh");
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_access"));

        await using var db = postgres.CreateContext();
        var tokens = await db.Set<RefreshToken>().Where(t => t.UserId == user.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        var rotated = tokens.Single(t => t.ReplacedById != null);
        var current = tokens.Single(t => t.ReplacedById == null);
        Assert.NotNull(rotated.RevokedAt);
        Assert.Equal(current.Id, rotated.ReplacedById);
        Assert.Null(current.RevokedAt);
        Assert.Equal(rotated.FamilyId, current.FamilyId);
        Assert.True(current.IsPersistent);
    }

    [Fact]
    public async Task Refresh_ReusedRotatedToken_Returns401AndRevokesTheWholeFamily()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password);
        var first = AuthHttp.CookieValue(login, "wz_refresh")!;
        var second = AuthHttp.CookieValue(await RefreshAsync(first), "wz_refresh")!;

        var reuse = await RefreshAsync(first);

        await AuthHttp.AssertProblemAsync(reuse, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second)).StatusCode);
        await using var db = postgres.CreateContext();
        Assert.All(await db.Set<RefreshToken>().Where(t => t.UserId == user.Id).ToListAsync(), t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password);
        _factory.Clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));

        var response = await RefreshAsync(AuthHttp.CookieValue(login, "wz_refresh")!);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/refresh", null);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401AndClearsCookies()
    {
        var response = await RefreshAsync("not-a-real-token");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        AuthHttp.AssertCleared(response, "wz_access");
        AuthHttp.AssertCleared(response, "wz_refresh");
    }

    [Fact]
    public async Task Refresh_DeactivatedUser_Returns401()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password);
        await using (var db = postgres.CreateContext())
        {
            await db.Set<User>().Where(u => u.Id == user.Id).ExecuteUpdateAsync(set => set.SetProperty(u => u.IsActive, false));
        }

        var response = await RefreshAsync(AuthHttp.CookieValue(login, "wz_refresh")!);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Logout (AUTH-04) ---

    [Fact]
    public async Task Logout_RevokesTheRefreshTokenAndClearsBothCookies()
    {
        var user = await UserAsync();
        var login = await LoginAsync(user.Email, Password);
        var refresh = AuthHttp.CookieValue(login, "wz_refresh")!;

        var response = await SendAsync(HttpMethod.Post, "/api/v1/auth/logout", ("wz_refresh", refresh));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AuthHttp.AssertCleared(response, "wz_access");
        AuthHttp.AssertCleared(response, "wz_refresh");
        Assert.NotNull((await SingleRefreshTokenAsync(user.Id)).RevokedAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refresh)).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookies_Returns204()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Rate limiting per IP (.ia/security.md §7) ---

    [Theory]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/resend-verification")]
    public async Task AnonymousEmailEndpoints_SixthRequestFromTheSameIpIn15Minutes_Returns429(string path)
    {
        var client = _factory.CreateClient();
        object Body() => new { fullName = "Ada", email = $"{Guid.NewGuid():N}@test.local", password = Password, campusId = _campus.Id };
        for (var request = 0; request < 5; request++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync(path, Body())).StatusCode);
        }

        var response = await client.PostAsJsonAsync(path, Body());

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.TooManyRequests, "RATE_LIMITED");
        Assert.True(int.Parse(response.Headers.GetValues("Retry-After").Single()) > 0);
    }

    // --- Helpers ---

    private async Task<User> UserAsync(bool verified = true, bool active = true)
    {
        var user = _schema.NewUser();
        user.CampusId = _campus.Id;
        user.IsActive = active;
        user.EmailVerifiedAt = verified ? _factory.Clock.UtcNow : null;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Password);
        await postgres.SaveAsync(user);
        return user;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password, bool rememberMe = false) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password, rememberMe });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        SendAsync(HttpMethod.Post, "/api/v1/auth/refresh", ("wz_refresh", refreshToken));

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, (string Name, string Value) cookie)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", $"{cookie.Name}={cookie.Value}");
        return _factory.CreateClient().SendAsync(request);
    }

    private async Task<RefreshToken> SingleRefreshTokenAsync(Guid userId)
    {
        await using var db = postgres.CreateContext();
        return await db.Set<RefreshToken>().AsNoTracking().SingleAsync(token => token.UserId == userId);
    }
}
