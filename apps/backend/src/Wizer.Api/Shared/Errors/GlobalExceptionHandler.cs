using Microsoft.AspNetCore.Diagnostics;

namespace Wizer.Api.Shared.Errors;

/// <summary>
/// Turns every exception into ProblemDetails and logs it once, following .ia/errors.md:
/// 5xx as Error with the exception, 403/429 as Warning, other 4xx are not logged.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = ProblemDetailsMapper.Map(exception);
        var status = problem.Status ?? StatusCodes.Status500InternalServerError;
        var code = problem.Extensions["code"];

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {RequestPath} with code {ErrorCode}", httpContext.Request.Path, code);
        }
        else if (status is StatusCodes.Status403Forbidden or StatusCodes.Status429TooManyRequests)
        {
            logger.LogWarning("Request to {RequestPath} rejected with {StatusCode} {ErrorCode}", httpContext.Request.Path, status, code);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
