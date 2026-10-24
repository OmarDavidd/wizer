using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.RateLimiting;

/// <summary>Per-IP limits for anonymous endpoints (.ia/security.md §7). Rejections are 429 RATE_LIMITED + Retry-After.</summary>
public static class RateLimitPolicies
{
    /// <summary>Register, resend verification and forgot password: 5 per IP and endpoint every 15 minutes.</summary>
    public const string AnonymousEmail = "anonymous-email";

    public static IServiceCollection AddWizerRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            // The client IP comes from the forwarded headers set by the proxy (infrastructure.md).
            options.AddPolicy(AnonymousEmail, context => RateLimitPartition.GetFixedWindowLimiter(
                $"{context.Connection.RemoteIpAddress}|{context.Request.Path}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
                }

                http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitPolicies))
                    .LogWarning("Request to {RequestPath} rejected with {StatusCode} {ErrorCode}",
                        http.Request.Path, StatusCodes.Status429TooManyRequests, ErrorCodes.RateLimited);

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Too many requests. Try again later.",
                };
                problem.Extensions["code"] = ErrorCodes.RateLimited;
                await http.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
            };
        });
}
