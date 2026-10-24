using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Shared.Auth;

namespace Wizer.IntegrationTests.Infrastructure.Authentication;

public class AuthenticationPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SigningKey = "integration-tests-signing-key-with-more-than-32-bytes";

    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationPipelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Jwt:SigningKey", SigningKey)
            .ConfigureTestServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(AuthProbeTestController).Assembly);
                services.AddScoped<IValidator<ProbeRequest>, ProbeRequestValidator>();
            }));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutCookie_Returns401ProblemDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/__test/auth/me");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidAccessCookie_ExposesCurrentUser()
    {
        var userId = Guid.NewGuid();
        var institutionId = Guid.NewGuid();
        var token = IssueToken(userId, PlatformRole.Teacher, institutionId);

        var response = await SendWithAccessCookieAsync("/__test/auth/me", token.Value);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(userId, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Teacher", body.RootElement.GetProperty("role").GetString());
        Assert.Equal(institutionId, body.RootElement.GetProperty("institutionId").GetGuid());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenInAuthorizationHeaderOnly_Returns401()
    {
        var token = IssueToken(Guid.NewGuid(), PlatformRole.Student, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, "/__test/auth/me");
        request.Headers.Authorization = new("Bearer", token.Value);

        var response = await _factory.CreateClient().SendAsync(request);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        var expired = CreateToken(SigningKey, expires: DateTime.UtcNow.AddMinutes(-5));

        var response = await SendWithAccessCookieAsync("/__test/auth/me", expired);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenSignedByAnotherKey_Returns401()
    {
        var forged = CreateToken("another-signing-key-that-is-also-longer-than-32-bytes", DateTime.UtcNow.AddMinutes(5));

        var response = await SendWithAccessCookieAsync("/__test/auth/me", forged);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminPolicy_WithStudentToken_Returns403()
    {
        var token = IssueToken(Guid.NewGuid(), PlatformRole.Student, Guid.NewGuid());

        var response = await SendWithAccessCookieAsync("/__test/auth/admin", token.Value);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    [Fact]
    public async Task AdminPolicy_WithAdminToken_Returns200()
    {
        var token = IssueToken(Guid.NewGuid(), PlatformRole.Admin, Guid.NewGuid());

        var response = await SendWithAccessCookieAsync("/__test/auth/admin", token.Value);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousEndpoint_WithoutCookie_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/__test/auth/anonymous");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestBody_IsRejectedByItsValidatorBeforeTheAction()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/__test/auth/validated", new { name = "" });

        var root = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.Equal("Name is required.", root.GetProperty("errors").GetProperty("name")[0].GetString());
    }

    [Fact]
    public async Task ValidRequestBody_ReachesTheAction()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/__test/auth/validated", new { name = "Ada" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private AccessToken IssueToken(Guid userId, PlatformRole role, Guid institutionId) =>
        _factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(userId, role, institutionId);

    private Task<HttpResponseMessage> SendWithAccessCookieAsync(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{AuthCookies.Access}={token}");
        return _factory.CreateClient().SendAsync(request);
    }

    private static string CreateToken(string key, DateTime expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(WizerClaimTypes.Subject, Guid.NewGuid().ToString()),
                new Claim(WizerClaimTypes.Role, "admin"),
                new Claim(WizerClaimTypes.Institution, Guid.NewGuid().ToString()),
            ]),
            Issuer = AccessTokenIssuer.Issuer,
            Audience = AccessTokenIssuer.Audience,
            NotBefore = expires.AddMinutes(-15),
            IssuedAt = expires.AddMinutes(-15),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256),
        });

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        return root;
    }
}
