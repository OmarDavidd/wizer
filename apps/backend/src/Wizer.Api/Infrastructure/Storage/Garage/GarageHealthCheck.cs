using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Wizer.Api.Infrastructure.Storage.Garage;

/// <summary>
/// Calls Garage's unauthenticated admin <c>/health</c>: 200 when the node can serve requests
/// (quorum reached), 503 otherwise.
/// </summary>
public sealed class GarageHealthCheck(HttpClient httpClient, IOptions<GarageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.Value.AdminEndpoint, UriKind.Absolute, out var endpoint))
        {
            return HealthCheckResult.Unhealthy("Garage admin endpoint is not configured.");
        }

        try
        {
            using var response = await httpClient.GetAsync(new Uri(endpoint, "/health"), cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Garage responded {(int)response.StatusCode}.");
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Unhealthy("Garage is not reachable.", exception);
        }
    }
}
