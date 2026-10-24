using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Identity;

[Collection(PostgresCollection.Name)]
public sealed partial class PasswordRecoveryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "contrasena123";
    private const string NewPassword = "nuevaclave456";

    private readonly WizerApiFactory _factory = new(postgres);
    private readonly SchemaBuilder _schema = new(postgres);
    private Campus _campus = null!;

    public async Task InitializeAsync()
    {
        await _schema.WithInstitutionAsync();
        _campus = await _schema.CampusAsync();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // --- Forgot password ---

    [Fact]
    public async Task ForgotPassword_ActiveAccount_Returns202AndEmailsAResetCode()
    {
        var user = await UserAsync();

        var response = await ForgotAsync(user.Email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        var email = (await _factory.Emails.WaitForAsync(user.Email)).Single();
        Assert.Contains("contraseña", email.Subject);
        Assert.Matches(SixDigits(), email.TextBody);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns202WithoutEmail()
    {
        var email = $"{Guid.NewGuid():N}@test.local";

        var response = await ForgotAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Empty(_factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task ForgotPassword_InactiveAccount_Returns202WithoutEmail()
    {
        var user = await UserAsync(active: false);

        var response = await ForgotAsync(user.Email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Empty(_factory.Emails.SentTo(user.Email));
    }

    [Fact]
    public async Task ForgotPassword_TwiceWithin60Seconds_SendsOneCode()
    {
        var user = await UserAsync();
        await ForgotAsync(user.Email);
        await _factory.Emails.WaitForAsync(user.Email);
        _factory.Clock.Advance(TimeSpan.FromSeconds(30));

        await ForgotAsync(user.Email);

        await InMemoryEmailSender.SettleAsync();
        Assert.Single(_factory.Emails.SentTo(user.Email));
    }

    // --- Reset password ---

    [Fact]
    public async Task ResetPassword_CorrectCode_Returns204AndReplacesThePasswordWithoutSigningIn()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);

        var response = await ResetAsync(user.Email, code, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(AuthHttp.SetCookies(response));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_RevokesEverySession()
    {
        var user = await UserAsync();
        var firstSession = AuthHttp.CookieValue(await LoginAsync(user.Email, Password), "wz_refresh")!;
        var secondSession = AuthHttp.CookieValue(await LoginAsync(user.Email, Password, rememberMe: true), "wz_refresh")!;
        var code = await RequestCodeAsync(user.Email);

        await ResetAsync(user.Email, code, NewPassword);

        await using var db = postgres.CreateContext();
        Assert.All(await db.Set<RefreshToken>().Where(t => t.UserId == user.Id).ToListAsync(), t => Assert.NotNull(t.RevokedAt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(firstSession)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(secondSession)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnverifiedAccount_MarksTheEmailVerified()
    {
        var user = await UserAsync(verified: false);
        var code = await RequestCodeAsync(user.Email);

        await ResetAsync(user.Email, code, NewPassword);

        await using var db = postgres.CreateContext();
        Assert.NotNull((await db.Set<User>().SingleAsync(u => u.Id == user.Id)).EmailVerifiedAt);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnlocksALockedLogin()
    {
        var user = await UserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(user.Email, "otraclave999");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(user.Email, Password)).StatusCode);
        var code = await RequestCodeAsync(user.Email);

        await ResetAsync(user.Email, code, NewPassword);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WrongCode_Returns400InvalidResetCodeAndKeepsThePassword()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);

        var response = await ResetAsync(user.Email, OtherCode(code), NewPassword);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AfterFiveWrongAttempts_RejectsTheCorrectCode()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await ResetAsync(user.Email, OtherCode(code), NewPassword);
        }

        var response = await ResetAsync(user.Email, code, NewPassword);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
    }

    [Fact]
    public async Task ResetPassword_ExpiredCode_Returns400()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);
        _factory.Clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        var response = await ResetAsync(user.Email, code, NewPassword);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
    }

    [Fact]
    public async Task ResetPassword_CodeUsedTwice_SecondTimeReturns400()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);
        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(user.Email, code, NewPassword)).StatusCode);

        var response = await ResetAsync(user.Email, code, "otraclave777");

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
    }

    [Fact]
    public async Task ResetPassword_EmailVerificationCode_IsNotAResetCode()
    {
        var user = await UserAsync(verified: false);
        await LoginAsync(user.Email, Password);
        var verificationCode = CodeFrom((await _factory.Emails.WaitForAsync(user.Email)).Single());

        var response = await ResetAsync(user.Email, verificationCode, NewPassword);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
    }

    [Fact]
    public async Task ResetPassword_UnknownEmail_Returns400InvalidResetCode()
    {
        var response = await ResetAsync($"{Guid.NewGuid():N}@test.local", "123456", NewPassword);

        await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_RESET_CODE");
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_Returns400ValidationError()
    {
        var user = await UserAsync();
        var code = await RequestCodeAsync(user.Email);

        var response = await ResetAsync(user.Email, code, "corta1");

        var root = await AuthHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("newPassword", out _));
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

    private async Task<string> RequestCodeAsync(string email)
    {
        var before = _factory.Emails.SentTo(email).Count;
        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(email)).StatusCode);
        return CodeFrom((await _factory.Emails.WaitForAsync(email, before + 1))[before]);
    }

    private Task<HttpResponseMessage> ForgotAsync(string email) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });

    private Task<HttpResponseMessage> ResetAsync(string email, string code, string newPassword) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/reset-password", new { email, code, newPassword });

    private Task<HttpResponseMessage> LoginAsync(string email, string password, bool rememberMe = false) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password, rememberMe });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"wz_refresh={refreshToken}");
        return _factory.CreateClient().SendAsync(request);
    }

    private static string CodeFrom(EmailMessage message) => SixDigits().Match(message.TextBody).Value;

    private static string OtherCode(string code) => code == "000000" ? "000001" : "000000";

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SixDigits();
}
