using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Identity;

[Collection(PostgresCollection.Name)]
public sealed partial class RegistrationTests(PostgresFixture postgres) : IAsyncLifetime
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

    // --- Register (AUTH-01, AUTH-05) ---

    [Fact]
    public async Task Register_NewEmail_Returns202AndCreatesUnverifiedStudentAndSendsCode()
    {
        var email = NewEmail();

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        var user = await FindUserAsync(email);
        Assert.NotNull(user);
        Assert.Equal(PlatformRole.Student, user.PlatformRole);
        Assert.Equal(_campus.Id, user.CampusId);
        Assert.Equal(_schema.InstitutionId, user.InstitutionId);
        Assert.Equal("Ada Lovelace", user.FullName);
        Assert.True(user.IsActive);
        Assert.Null(user.EmailVerifiedAt);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash!, Password));
        Assert.Matches(SixDigits(), CodeFrom((await _factory.Emails.WaitForAsync(email)).Single()));
    }

    [Fact]
    public async Task Register_EmailWithSpacesAndUppercase_IsStoredNormalized()
    {
        var email = NewEmail();

        await RegisterAsync($"  {email.ToUpperInvariant()} ");

        Assert.NotNull(await FindUserAsync(email));
        await _factory.Emails.WaitForAsync(email);
    }

    [Fact]
    public async Task Register_WithRoleInBody_StillCreatesStudent()
    {
        var email = NewEmail();

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "Ada Lovelace",
            email,
            password = Password,
            campusId = _campus.Id,
            role = "admin",
            platformRole = "admin",
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(PlatformRole.Student, (await FindUserAsync(email))!.PlatformRole);
    }

    [Fact]
    public async Task Register_ExistingVerifiedEmail_Returns202WithoutEmailAndKeepsAccount()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var before = await FindUserAsync(email);

        var response = await RegisterAsync(email, fullName: "Someone Else", password: "otraclave999");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Single(_factory.Emails.SentTo(email));
        var after = await FindUserAsync(email);
        Assert.Equal(before!.FullName, after!.FullName);
        Assert.Equal(before.PasswordHash, after.PasswordHash);
    }

    [Fact]
    public async Task Register_ExistingUnverifiedEmail_AfterCooldown_SendsNewCodeAndKeepsOriginalPassword()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var firstCode = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        _factory.Clock.Advance(TimeSpan.FromSeconds(61));

        var response = await RegisterAsync(email, password: "otraclave999");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var secondCode = CodeFrom((await _factory.Emails.WaitForAsync(email, 2))[1]);
        var verify = await VerifyAsync(email, secondCode, "otraclave999");
        await AssertProblemAsync(verify, HttpStatusCode.BadRequest, "VERIFICATION_PASSWORD_MISMATCH");
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(email, secondCode, Password)).StatusCode);
        Assert.NotEqual(firstCode, secondCode);
    }

    [Theory]
    [InlineData("corta1", "too short")]
    [InlineData("sololetrasaqui", "no digit")]
    [InlineData("1234567890", "no letter")]
    public async Task Register_WeakPassword_Returns400ValidationError(string password, string reason)
    {
        var response = await RegisterAsync(NewEmail(), password: password);

        var root = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("password", out _), reason);
    }

    [Fact]
    public async Task Register_PasswordEqualToEmail_Returns400ValidationError()
    {
        var email = $"{Guid.NewGuid():N}1@test.local";

        var response = await RegisterAsync(email, password: email);

        var root = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400ValidationError()
    {
        var response = await RegisterAsync("not-an-email");

        var root = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Register_InactiveCampus_Returns400AndCreatesNothing()
    {
        var inactive = await _schema.CampusAsync(isActive: false);
        var email = NewEmail();

        var response = await RegisterAsync(email, campusId: inactive.Id);

        var root = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.True(root.GetProperty("errors").TryGetProperty("campusId", out _));
        Assert.Null(await FindUserAsync(email));
    }

    [Fact]
    public async Task Register_UnknownCampus_Returns400()
    {
        var response = await RegisterAsync(NewEmail(), campusId: Guid.NewGuid());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    // --- Verify email (AUTH-07) ---

    [Fact]
    public async Task VerifyEmail_CorrectCodeAndPassword_Returns200WithSessionCookiesAndVerifiesAccount()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());

        var response = await VerifyAsync(email, code, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal("student", body.RootElement.GetProperty("role").GetString());

        var cookies = SetCookies(response);
        AssertCookie(cookies, "wz_access", "/");
        AssertCookie(cookies, "wz_refresh", "/api/v1/auth");

        var user = await FindUserAsync(email);
        Assert.Equal(_factory.Clock.UtcNow, user!.EmailVerifiedAt);
        await using var db = postgres.CreateContext();
        var refreshToken = await db.Set<RefreshToken>().SingleAsync(token => token.UserId == user.Id);
        Assert.NotEqual(CookieValue(cookies, "wz_refresh"), refreshToken.TokenHash);
        Assert.Equal(64, refreshToken.TokenHash.Length);
        Assert.False(refreshToken.IsPersistent);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(7), refreshToken.ExpiresAt);
        Assert.NotNull((await db.Set<VerificationCode>().SingleAsync(c => c.UserId == user.Id)).ConsumedAt);
    }

    [Fact]
    public async Task VerifyEmail_WrongCode_Returns400InvalidCode()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());

        var response = await VerifyAsync(email, OtherCode(code), Password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
        Assert.Null((await FindUserAsync(email))!.EmailVerifiedAt);
    }

    [Fact]
    public async Task VerifyEmail_AfterFiveWrongAttempts_RejectsEvenTheCorrectCode()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await VerifyAsync(email, OtherCode(code), Password);
        }

        var response = await VerifyAsync(email, code, Password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
    }

    [Fact]
    public async Task VerifyEmail_ConcurrentWrongAttempts_NeverExceedFiveReservations()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());

        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => VerifyAsync(email, OtherCode(code), Password)));

        await using var db = postgres.CreateContext();
        var user = await FindUserAsync(email);
        Assert.Equal(5, (await db.Set<VerificationCode>().SingleAsync(c => c.UserId == user!.Id)).Attempts);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredCode_Returns400InvalidCode()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        _factory.Clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        var response = await VerifyAsync(email, code, Password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
    }

    [Fact]
    public async Task VerifyEmail_CodeUsedTwice_SecondTimeReturns400()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(email, code, Password)).StatusCode);

        var response = await VerifyAsync(email, code, Password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
    }

    [Fact]
    public async Task VerifyEmail_ValidCodeWithOtherPassword_Returns400PasswordMismatchAndKeepsCode()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());

        var response = await VerifyAsync(email, code, "otraclave999");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VERIFICATION_PASSWORD_MISMATCH");
        Assert.Empty(SetCookies(response));
        Assert.Null((await FindUserAsync(email))!.EmailVerifiedAt);
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(email, code, Password)).StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_UnknownEmail_Returns400InvalidCode()
    {
        var response = await VerifyAsync(NewEmail(), "123456", Password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
    }

    // --- Resend verification ---

    [Fact]
    public async Task ResendVerification_UnknownEmail_Returns202WithoutEmail()
    {
        var email = NewEmail();

        var response = await ResendAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Empty(_factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task ResendVerification_Within60Seconds_DoesNotSendAnotherCode()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        await _factory.Emails.WaitForAsync(email);
        _factory.Clock.Advance(TimeSpan.FromSeconds(59));

        var response = await ResendAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Single(_factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task ResendVerification_After60Seconds_SendsNewCodeAndInvalidatesThePreviousOne()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var firstCode = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        _factory.Clock.Advance(TimeSpan.FromSeconds(60));

        await ResendAsync(email);

        var secondCode = CodeFrom((await _factory.Emails.WaitForAsync(email, 2))[1]);
        if (firstCode != secondCode)
        {
            await AssertProblemAsync(await VerifyAsync(email, firstCode, Password), HttpStatusCode.BadRequest, "INVALID_VERIFICATION_CODE");
        }

        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(email, secondCode, Password)).StatusCode);
    }

    [Fact]
    public async Task ResendVerification_VerifiedAccount_Returns202WithoutEmail()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        _factory.Clock.Advance(TimeSpan.FromMinutes(2));

        var response = await ResendAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await InMemoryEmailSender.SettleAsync();
        Assert.Single(_factory.Emails.SentTo(email));
    }

    // --- Helpers ---

    private static string NewEmail() => $"{Guid.NewGuid():N}@test.local";

    private Task<HttpResponseMessage> RegisterAsync(
        string email,
        string fullName = "Ada Lovelace",
        string password = Password,
        Guid? campusId = null) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName,
            email,
            password,
            campusId = campusId ?? _campus.Id,
        });

    private Task<HttpResponseMessage> VerifyAsync(string email, string code, string password) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/verify-email", new { email, code, password });

    private Task<HttpResponseMessage> ResendAsync(string email) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/resend-verification", new { email });

    private async Task<HttpResponseMessage> RegisterAndVerifyAsync(string email)
    {
        await RegisterAsync(email);
        var code = CodeFrom((await _factory.Emails.WaitForAsync(email)).Single());
        var response = await VerifyAsync(email, code, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    private async Task<User?> FindUserAsync(string email)
    {
        await using var db = postgres.CreateContext();
        return await db.Set<User>().AsNoTracking().SingleOrDefaultAsync(user => user.Email == email);
    }

    private static string CodeFrom(Wizer.Api.Infrastructure.Email.EmailMessage message) =>
        SixDigits().Match(message.TextBody).Value;

    private static string OtherCode(string code) => code == "000000" ? "000001" : "000000";

    private static IReadOnlyList<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    private static string CookieValue(IReadOnlyList<string> cookies, string name) =>
        cookies.Single(cookie => cookie.StartsWith(name + "=", StringComparison.Ordinal)).Split(';')[0][(name.Length + 1)..];

    private static void AssertCookie(IReadOnlyList<string> cookies, string name, string path)
    {
        var cookie = cookies.Single(value => value.StartsWith(name + "=", StringComparison.Ordinal)).ToLowerInvariant();
        Assert.Contains($"path={path}", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("expires=", cookie);
    }

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        return root;
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SixDigits();
}
