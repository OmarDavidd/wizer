using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wizer.IntegrationTests.Shared;

public class HealthReadyTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthReady_DependenciesUnreachable_Returns503WithStatusOnly()
    {
        // Port 1 on loopback refuses connections, so every dependency fails fast.
        var client = factory.WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:Wizer", "Host=127.0.0.1;Port=1;Database=wizer;Timeout=2")
                .UseSetting("Minio:Endpoint", "http://127.0.0.1:1")
                .UseSetting("ClamAv:Host", "127.0.0.1")
                .UseSetting("ClamAv:Port", "1"))
            .CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HealthReady_DependenciesNotConfigured_Returns503WithStatusOnly()
    {
        var client = factory.WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:Wizer", "")
                .UseSetting("Minio:Endpoint", "")
                .UseSetting("ClamAv:Host", ""))
            .CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }
}
