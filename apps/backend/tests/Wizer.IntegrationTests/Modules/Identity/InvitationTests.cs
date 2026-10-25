using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Identity;

[Collection(PostgresCollection.Name)]
public sealed partial class InvitationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string PublicUrl = "https://wizer.test";
    private const string Password = "contrasena123";

    private readonly WizerApiFactory _factory = new(postgres);
    private readonly SchemaBuilder _schema = new(postgres);
    private WebApplicationFactory<Program> _app = null!;
    private Campus _campus = null!;
    private User _admin = null!;

    public async Task InitializeAsync()
    {
        await _schema.WithInstitutionAsync();
        _campus = await _schema.CampusAsync();
        _admin = await _schema.UserAsync(PlatformRole.Admin);
        _app = _factory.WithWebHostBuilder(builder => builder.UseSetting("App:PublicUrl", PublicUrl));
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        await _factory.DisposeAsync();
    }

    // --- Invite (AUTH-09) ---

    [Fact]
    public async Task Invite_AsAdmin_CreatesInactiveUserWithoutPasswordAndEmailsA72HourLink()
    {
        var email = NewEmail();

        var response = await InviteAsync(email, "teacher");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal("teacher", body.RootElement.GetProperty("role").GetString());

        var user = await FindUserAsync(email);
        Assert.NotNull(user);
        Assert.Equal(PlatformRole.Teacher, user.PlatformRole);
        Assert.False(user.IsActive);
        Assert.Null(user.PasswordHash);
        Assert.Null(user.EmailVerifiedAt);
        Assert.Equal(_campus.Id, user.CampusId);

        var token = TokenFrom((await _factory.Emails.WaitForAsync(email)).Single());
        await using var db = postgres.CreateContext();
        var invitation = await db.Set<InvitationToken>().SingleAsync(i => i.UserId == user.Id);
        Assert.NotEqual(token, invitation.TokenHash);
        Assert.Equal(_admin.Id, invitation.InvitedById);
        Assert.Equal(_factory.Clock.UtcNow.AddHours(72), invitation.ExpiresAt);
    }

    [Fact]
    public async Task Invite_Admin_CreatesAnAdminAccount()
    {
        var email = NewEmail();

        Assert.Equal(HttpStatusCode.Created, (await InviteAsync(email, "admin")).StatusCode);

        Assert.Equal(PlatformRole.Admin, (await FindUserAsync(email))!.PlatformRole);
    }

    [Fact]
    public async Task Invite_ExistingEmail_Returns409EmailAlreadyExists()
    {
        var existing = await _schema.UserAsync();

        var response = await InviteAsync(existing.Email.ToUpperInvariant(), "teacher");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Conflict, "EMAIL_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Invite_StudentRole_Returns400()
    {
        var response = await InviteAsync(NewEmail(), "student");

        var root = await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("role", out _));
    }

    [Fact]
    public async Task Invite_InactiveCampus_Returns400()
    {
        var inactive = await _schema.CampusAsync(isActive: false);

        var response = await InviteAsync(NewEmail(), "teacher", inactive.Id);

        var root = await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("campusId", out _));
    }

    [Theory]
    [InlineData(PlatformRole.Student)]
    [InlineData(PlatformRole.Teacher)]
    public async Task Invite_NotAdmin_Returns403(PlatformRole role)
    {
        var caller = await _schema.UserAsync(role);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/users", caller,
            new { fullName = "Laura Ríos", email = NewEmail(), role = "teacher", campusId = _campus.Id });

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    [Fact]
    public async Task Invite_Anonymous_Returns401()
    {
        var response = await _app.CreateClient().PostAsJsonAsync("/api/v1/admin/users",
            new { fullName = "Laura Ríos", email = NewEmail(), role = "teacher", campusId = _campus.Id });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Resend ---

    [Fact]
    public async Task Resend_RevokesThePreviousLinkAndSendsANewOne()
    {
        var email = NewEmail();
        var userId = await InvitedIdAsync(email);
        var first = TokenFrom((await _factory.Emails.WaitForAsync(email)).Single());

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/users/{userId}/resend-invitation", _admin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var second = TokenFrom((await _factory.Emails.WaitForAsync(email, 2))[1]);
        Assert.NotEqual(first, second);
        await AuthHttp.AssertProblemAsync(await SetPasswordAsync(first), HttpStatusCode.BadRequest, "INVALID_OR_EXPIRED_TOKEN");
        Assert.Equal(HttpStatusCode.OK, (await SetPasswordAsync(second)).StatusCode);
    }

    [Fact]
    public async Task Resend_AlreadyActivatedUser_Returns409()
    {
        var email = NewEmail();
        var userId = await InvitedIdAsync(email);
        await SetPasswordAsync(TokenFrom((await _factory.Emails.WaitForAsync(email)).Single()));

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/users/{userId}/resend-invitation", _admin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Resend_DeactivatedGoogleStudent_Returns409AndSendsNothing()
    {
        var student = _schema.NewUser(PlatformRole.Student);
        student.CampusId = _campus.Id;
        student.IsActive = false;
        student.EmailVerifiedAt = _factory.Clock.UtcNow;
        await postgres.SaveAsync(student);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/users/{student.Id}/resend-invitation", _admin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Empty(_factory.Emails.SentTo(student.Email));
        Assert.False((await FindUserAsync(student.Email))!.IsActive);
    }

    [Fact]
    public async Task Resend_UnknownUser_Returns404()
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/users/{Guid.NewGuid()}/resend-invitation", _admin);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Set password ---

    [Fact]
    public async Task SetPassword_ValidToken_ActivatesVerifiesAndSignsIn()
    {
        var email = NewEmail();
        await InvitedIdAsync(email);
        var token = TokenFrom((await _factory.Emails.WaitForAsync(email)).Single());

        var response = await SetPasswordAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("teacher", body.RootElement.GetProperty("role").GetString());
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_access"));
        Assert.NotNull(AuthHttp.CookieValue(response, "wz_refresh"));

        var user = await FindUserAsync(email);
        Assert.True(user!.IsActive);
        Assert.NotNull(user.EmailVerifiedAt);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash!, Password));
        var login = await _app.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task SetPassword_ExpiredToken_Returns400()
    {
        var email = NewEmail();
        await InvitedIdAsync(email);
        var token = TokenFrom((await _factory.Emails.WaitForAsync(email)).Single());
        _factory.Clock.Advance(TimeSpan.FromHours(72) + TimeSpan.FromSeconds(1));

        var response = await SetPasswordAsync(token);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_OR_EXPIRED_TOKEN");
        Assert.False((await FindUserAsync(email))!.IsActive);
    }

    [Fact]
    public async Task SetPassword_TokenUsedTwice_SecondTimeReturns400()
    {
        var email = NewEmail();
        await InvitedIdAsync(email);
        var token = TokenFrom((await _factory.Emails.WaitForAsync(email)).Single());
        Assert.Equal(HttpStatusCode.OK, (await SetPasswordAsync(token)).StatusCode);

        var response = await SetPasswordAsync(token, "otraclave999");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_OR_EXPIRED_TOKEN");
    }

    [Fact]
    public async Task SetPassword_UnknownToken_Returns400()
    {
        var response = await SetPasswordAsync("not-a-real-token");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_OR_EXPIRED_TOKEN");
    }

    [Fact]
    public async Task SetPassword_WeakPassword_Returns400ValidationError()
    {
        var response = await SetPasswordAsync("any-token", "corta1");

        var root = await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("password", out _));
    }

    // --- Helpers ---

    private static string NewEmail() => $"{Guid.NewGuid():N}@uni.test";

    private Task<HttpResponseMessage> InviteAsync(string email, string role, Guid? campusId = null) =>
        SendAsync(HttpMethod.Post, "/api/v1/admin/users", _admin,
            new { fullName = "Laura Ríos", email, role, campusId = campusId ?? _campus.Id });

    private async Task<Guid> InvitedIdAsync(string email)
    {
        var response = await InviteAsync(email, "teacher");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> SetPasswordAsync(string token, string password = Password) =>
        _app.CreateClient().PostAsJsonAsync("/api/v1/auth/set-password", new { token, password });

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, User caller, object? body = null)
    {
        var accessToken = _app.Services.GetRequiredService<IAccessTokenIssuer>()
            .Issue(caller.Id, caller.PlatformRole, caller.InstitutionId);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", $"wz_access={accessToken.Value}");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return _app.CreateClient().SendAsync(request);
    }

    private async Task<User?> FindUserAsync(string email)
    {
        await using var db = postgres.CreateContext();
        return await db.Set<User>().AsNoTracking().SingleOrDefaultAsync(user => user.Email == email);
    }

    private static string TokenFrom(EmailMessage message)
    {
        var match = ActivationLink().Match(message.TextBody);
        Assert.True(match.Success, "The email has no activation link.");
        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }

    [GeneratedRegex(@"https://wizer\.test/activar\?token=(?<token>[A-Za-z0-9_%\-]+)")]
    private static partial Regex ActivationLink();
}
