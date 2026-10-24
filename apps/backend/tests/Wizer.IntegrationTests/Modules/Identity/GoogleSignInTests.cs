using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wizer.Api.Infrastructure.Authentication.Google;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Identity;

[Collection(PostgresCollection.Name)]
public sealed class GoogleSignInTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "contrasena123";

    private readonly WizerApiFactory _factory = new(postgres);
    private readonly SchemaBuilder _schema = new(postgres);
    private WebApplicationFactory<Program> _app = null!;
    private Campus _campus = null!;

    public async Task InitializeAsync()
    {
        await _schema.WithInstitutionAsync();
        _campus = await _schema.CampusAsync();
        _app = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();
        }));
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Google_InvalidToken_Returns401GoogleTokenInvalid()
    {
        var response = await GoogleAsync("garbage");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "GOOGLE_TOKEN_INVALID");
        Assert.Empty(AuthHttp.SetCookies(response));
    }

    [Fact]
    public async Task Google_NewAccountWithoutCampus_Returns400CampusRequiredAndCreatesNothing()
    {
        var email = NewEmail();

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(NewSub(), email));

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "CAMPUS_REQUIRED");
        Assert.Null(await FindUserAsync(email));
    }

    [Fact]
    public async Task Google_NewAccountWithCampus_CreatesVerifiedStudentWithoutPasswordAndSignsIn()
    {
        var email = NewEmail();
        var sub = NewSub();

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(sub, email), campusId: _campus.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_access"));
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_refresh"));
        var user = await FindUserAsync(email);
        Assert.NotNull(user);
        Assert.Equal(PlatformRole.Student, user.PlatformRole);
        Assert.Equal("Ana Pérez", user.FullName);
        Assert.Equal(_campus.Id, user.CampusId);
        Assert.Equal(_schema.InstitutionId, user.InstitutionId);
        Assert.NotNull(user.EmailVerifiedAt);
        Assert.Null(user.PasswordHash);
        Assert.True(user.IsActive);
        await using var db = postgres.CreateContext();
        var link = await db.Set<ExternalLogin>().SingleAsync(login => login.UserId == user.Id);
        Assert.Equal(ExternalLoginProvider.Google, link.Provider);
        Assert.Equal(sub, link.ProviderKey);
    }

    [Fact]
    public async Task Google_LinkedAccount_SignsInWithoutCampus()
    {
        var email = NewEmail();
        var sub = NewSub();
        await GoogleAsync(FakeGoogleTokenValidator.Token(sub, email), campusId: _campus.Id);

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(sub, email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Google_ExistingVerifiedLocalAccount_LinksItAndKeepsThePassword()
    {
        var user = await LocalUserAsync(verified: true);
        var sub = NewSub();

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(sub, user.Email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = postgres.CreateContext();
        Assert.Equal(sub, (await db.Set<ExternalLogin>().SingleAsync(login => login.UserId == user.Id)).ProviderKey);
        Assert.Equal(1, await db.Set<User>().CountAsync(u => u.Email == user.Email));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task Google_ExistingUnverifiedLocalAccount_Returns409WithoutLinking()
    {
        var user = await LocalUserAsync(verified: false);

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(NewSub(), user.Email), campusId: _campus.Id);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Conflict, "EMAIL_ALREADY_EXISTS");
        await using var db = postgres.CreateContext();
        Assert.False(await db.Set<ExternalLogin>().AnyAsync(login => login.UserId == user.Id));
    }

    [Fact]
    public async Task Google_InactiveLinkedAccount_Returns403AccountInactive()
    {
        var user = await LocalUserAsync(verified: true, active: false);

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(NewSub(), user.Email));

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Forbidden, "ACCOUNT_INACTIVE");
        await using var db = postgres.CreateContext();
        Assert.False(await db.Set<ExternalLogin>().AnyAsync(login => login.UserId == user.Id));
    }

    [Fact]
    public async Task Google_NewAccountWithInactiveCampus_Returns400()
    {
        var inactive = await _schema.CampusAsync(isActive: false);

        var response = await GoogleAsync(FakeGoogleTokenValidator.Token(NewSub(), NewEmail()), campusId: inactive.Id);

        var root = await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("campusId", out _));
    }

    [Fact]
    public async Task Google_RememberMe_KeepsTheSessionFor30Days()
    {
        var email = NewEmail();

        await GoogleAsync(FakeGoogleTokenValidator.Token(NewSub(), email), campusId: _campus.Id, rememberMe: true);

        var user = await FindUserAsync(email);
        await using var db = postgres.CreateContext();
        var token = await db.Set<RefreshToken>().SingleAsync(t => t.UserId == user!.Id);
        Assert.True(token.IsPersistent);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(30), token.ExpiresAt);
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@gmail.com";

    private static string NewSub() => Guid.NewGuid().ToString("N");

    private Task<HttpResponseMessage> GoogleAsync(string idToken, Guid? campusId = null, bool rememberMe = false) =>
        _app.CreateClient().PostAsJsonAsync("/api/v1/auth/google", new { idToken, rememberMe, campusId });

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _app.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password, rememberMe = false });

    private async Task<User> LocalUserAsync(bool verified, bool active = true)
    {
        var user = _schema.NewUser();
        user.CampusId = _campus.Id;
        user.IsActive = active;
        user.EmailVerifiedAt = verified ? _factory.Clock.UtcNow : null;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Password);
        await postgres.SaveAsync(user);
        return user;
    }

    private async Task<User?> FindUserAsync(string email)
    {
        await using var db = postgres.CreateContext();
        return await db.Set<User>().AsNoTracking().SingleOrDefaultAsync(user => user.Email == email);
    }

    /// <summary>Accepts tokens shaped <c>google|{sub}|{email}|{name}</c>; anything else is invalid.</summary>
    private sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
    {
        public static string Token(string sub, string email) => $"google|{sub}|{email}|Ana Pérez";

        public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            var parts = idToken.Split('|');
            return Task.FromResult(parts is ["google", var sub, var email, var name] ? new GoogleIdentity(sub, email, name) : null);
        }
    }
}
