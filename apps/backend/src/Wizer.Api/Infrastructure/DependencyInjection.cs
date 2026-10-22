using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Wizer.Api.Infrastructure.Antivirus.ClamAV;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Infrastructure.Storage.MinIO;

namespace Wizer.Api.Infrastructure;

public static class DependencyInjection
{
    private const string ReadyTag = "ready";
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));

        services.AddHttpClient<MinioHealthCheck>(client => client.Timeout = HealthCheckTimeout);

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: [ReadyTag], timeout: HealthCheckTimeout)
            .AddCheck<MinioHealthCheck>("minio", tags: [ReadyTag], timeout: HealthCheckTimeout)
            .AddCheck<ClamAvHealthCheck>("clamav", tags: [ReadyTag], timeout: HealthCheckTimeout);

        return services;
    }

    /// <summary>
    /// <c>/health/live</c>: the process responds. <c>/health/ready</c>: PostgreSQL, MinIO and ClamAV
    /// are reachable. Both are anonymous and only return the status, without internal details.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();
        return endpoints;
    }
}
