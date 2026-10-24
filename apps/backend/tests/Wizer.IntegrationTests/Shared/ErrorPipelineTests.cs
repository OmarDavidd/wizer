using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Wizer.IntegrationTests.Shared;

public class ErrorPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CorrelationHeader = "X-Correlation-Id";

    private readonly WebApplicationFactory<Program> _factory;

    public ErrorPipelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly)));
    }

    [Theory]
    [InlineData("unexpected", HttpStatusCode.InternalServerError, "INTERNAL_ERROR")]
    [InlineData("business", HttpStatusCode.BadRequest, "CLASS_ARCHIVED")]
    [InlineData("validation", HttpStatusCode.BadRequest, "VALIDATION_ERROR")]
    [InlineData("payload-too-large", HttpStatusCode.RequestEntityTooLarge, "PAYLOAD_TOO_LARGE")]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS")]
    [InlineData("too-many-requests", HttpStatusCode.TooManyRequests, "TOO_MANY_LOGIN_ATTEMPTS")]
    public async Task ThrownException_ReturnsProblemDetailsWithCodeAndCorrelationId(
        string scenario,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var response = await _factory.CreateClient().GetAsync($"/__test/throw/{scenario}");

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(expectedCode, root.GetProperty("code").GetString());
        Assert.Equal((int)expectedStatus, root.GetProperty("status").GetInt32());
        Assert.Equal(
            "https://wizer.app/errors/" + expectedCode.ToLowerInvariant().Replace('_', '-'),
            root.GetProperty("type").GetString());
        Assert.Equal($"/__test/throw/{scenario}", root.GetProperty("instance").GetString());
        Assert.Equal(
            response.Headers.GetValues(CorrelationHeader).Single(),
            root.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task TooManyRequestsException_SetsRetryAfterInWholeSecondsRoundedUp()
    {
        var response = await _factory.CreateClient().GetAsync("/__test/throw/too-many-requests");

        Assert.Equal("91", response.Headers.GetValues("Retry-After").Single());
    }

    [Fact]
    public async Task UnexpectedException_DoesNotLeakInternalDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/__test/throw/unexpected");

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("super-secret", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain("   at ", body);
    }

    [Fact]
    public async Task ValidationException_ReturnsCamelCaseFieldErrors()
    {
        var response = await _factory.CreateClient().GetAsync("/__test/throw/validation");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal("Name is required.", errors.GetProperty("name")[0].GetString());
    }
}
