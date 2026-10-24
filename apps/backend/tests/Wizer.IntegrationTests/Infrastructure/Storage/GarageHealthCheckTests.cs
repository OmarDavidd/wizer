using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Wizer.Api.Infrastructure.Storage.Garage;

namespace Wizer.IntegrationTests.Infrastructure.Storage;

/// <summary>Runs the image and the <c>infra/garage/garage.toml</c> that <c>docker-compose.dev.yml</c> uses.</summary>
public sealed class GarageHealthCheckTests : IAsyncLifetime
{
    private const int AdminPort = 3903;

    private readonly IContainer _garage = new ContainerBuilder("dxflrs/garage:v2.4.1")
        .WithCommand("/garage", "server", "--single-node", "--default-bucket")
        .WithResourceMapping(new FileInfo(FindGarageConfig()), "/etc/")
        .WithEnvironment("GARAGE_RPC_SECRET", new string('0', 63) + "1")
        .WithEnvironment("GARAGE_DEFAULT_ACCESS_KEY", "GK" + new string('0', 23) + "1")
        .WithEnvironment("GARAGE_DEFAULT_SECRET_KEY", new string('0', 63) + "1")
        .WithEnvironment("GARAGE_DEFAULT_BUCKET", "wizer")
        .WithPortBinding(AdminPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(AdminPort).ForPath("/health")))
        .Build();

    public Task InitializeAsync() => _garage.StartAsync();

    public Task DisposeAsync() => _garage.DisposeAsync().AsTask();

    [Fact]
    public async Task CheckHealthAsync_RunningGarage_ReturnsHealthy()
    {
        var adminEndpoint = $"http://{_garage.Hostname}:{_garage.GetMappedPublicPort(AdminPort)}";

        var result = await NewCheck(adminEndpoint).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_EndpointNotConfigured_ReturnsUnhealthy()
    {
        var result = await NewCheck("").CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static GarageHealthCheck NewCheck(string adminEndpoint) =>
        new(new HttpClient(), Options.Create(new GarageOptions { AdminEndpoint = adminEndpoint }));

    private static string FindGarageConfig()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "infra", "garage", "garage.toml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("infra/garage/garage.toml not found above the test output directory.");
    }
}
