using Serilog.Context;

namespace Wizer.Api.Shared.Observability;

/// <summary>
/// Reuses a valid incoming <c>X-Correlation-Id</c> or generates one, exposes it as
/// <see cref="HttpContext.TraceIdentifier"/>, returns it in the response and adds it to every log.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationId.HeaderName].ToString();
        var correlationId = CorrelationId.IsValid(incoming) ? incoming : CorrelationId.New();

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
