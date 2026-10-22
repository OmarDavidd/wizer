using System.Security.Claims;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Shared.Observability;

public static class LoggingSetup
{
    /// <summary>
    /// Serilog writes to stdout only: readable text in Development, compact JSON elsewhere.
    /// Shipping logs to Loki is Grafana Alloy's job, never the application's (.ia/observability.md).
    /// </summary>
    public static WebApplicationBuilder AddWizerLogging(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var environmentName = builder.Environment.EnvironmentName;

        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "wizer-api")
                .Enrich.WithProperty("Environment", environmentName)
                .Enrich.WithProperty("MachineName", Environment.MachineName);

            if (isDevelopment)
            {
                configuration.WriteTo.Console();
            }
            else
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
        });
        return builder;
    }

    /// <summary>One line per request with the route template, status, duration and user. No bodies, no headers.</summary>
    public static IApplicationBuilder UseWizerRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RoutePattern} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (context, _, exception) =>
                exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogEventLevel.Error
                    : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, context) =>
            {
                diagnostics.Set(
                    "RoutePattern",
                    (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value);

                var userId = context.User.FindFirstValue(WizerClaimTypes.Subject);
                if (userId is not null)
                {
                    diagnostics.Set("UserId", userId);
                }
            };
        });
}
