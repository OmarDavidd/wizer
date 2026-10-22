using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wizer.IntegrationTests.Shared;

public class SkeletonEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CorrelationHeader = "X-Correlation-Id";

    [Fact]
    public async Task HealthLive_Anonymous_Returns200()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnyRequest_WithValidCorrelationId_EchoesIt()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationHeader, "01JAB3CDEFGHJKMNPQRSTVWXYZ");

        var response = await client.SendAsync(request);

        Assert.Equal("01JAB3CDEFGHJKMNPQRSTVWXYZ", response.Headers.GetValues(CorrelationHeader).Single());
    }

    [Fact]
    public async Task AnyRequest_WithInvalidCorrelationId_GeneratesNewOne()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationHeader, "not-a-valid-id");

        var response = await client.SendAsync(request);

        var returned = response.Headers.GetValues(CorrelationHeader).Single();
        Assert.NotEqual("not-a-valid-id", returned);
        Assert.True(Guid.TryParse(returned, out _));
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetailsWithCodeAndCorrelationId()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal("RESOURCE_NOT_FOUND", root.GetProperty("code").GetString());
        Assert.Equal("https://wizer.app/errors/resource-not-found", root.GetProperty("type").GetString());
        Assert.Equal("/api/v1/does-not-exist", root.GetProperty("instance").GetString());
        Assert.Equal(
            response.Headers.GetValues(CorrelationHeader).Single(),
            root.GetProperty("correlationId").GetString());
        Assert.False(root.TryGetProperty("traceId", out _));
    }
}
