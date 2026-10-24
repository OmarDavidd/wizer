using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Wizer.Api.Infrastructure.Antivirus.ClamAV;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Infrastructure.Persistence.Seed;
using Wizer.Api.Infrastructure.Storage.Garage;
using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Infrastructure;

public static class DependencyInjection
{
    private const string ReadyTag = "ready";
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GarageOptions>(configuration.GetSection(GarageOptions.SectionName));
        services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));

        services.AddWizerAuthentication(configuration);
        services.AddWizerEmail(configuration);

        services.AddSingleton<AuditInterceptor>();
        // The connection string is read when a context is created, never at startup, so the
        // OpenAPI build step does not need a database.
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("Wizer");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'Wizer' is not configured.");
            }

            AppDbContext.Configure(options, connectionString, provider.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DevSeeder>();

        services.AddHttpClient<GarageHealthCheck>(client => client.Timeout = HealthCheckTimeout);

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: [ReadyTag], timeout: HealthCheckTimeout)
            .AddCheck<GarageHealthCheck>("garage", tags: [ReadyTag], timeout: HealthCheckTimeout)
            .AddCheck<ClamAvHealthCheck>("clamav", tags: [ReadyTag], timeout: HealthCheckTimeout);

        return services;
    }

    /// <summary>
    /// <c>/health/live</c>: the process responds. <c>/health/ready</c>: PostgreSQL, Garage and ClamAV
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
