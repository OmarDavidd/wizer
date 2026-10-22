using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Wizer.Api.Infrastructure.Storage.MinIO;

/// <summary>Calls MinIO's unauthenticated liveness endpoint.</summary>
public sealed class MinioHealthCheck(HttpClient httpClient, IOptions<MinioOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.Value.Endpoint, UriKind.Absolute, out var endpoint))
        {
            return HealthCheckResult.Unhealthy("MinIO endpoint is not configured.");
        }

        try
        {
            using var response = await httpClient.GetAsync(new Uri(endpoint, "/minio/health/live"), cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"MinIO responded {(int)response.StatusCode}.");
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Unhealthy("MinIO is not reachable.", exception);
        }
    }
}
