namespace Wizer.Api.Shared.Errors;

public static class DependencyInjection
{
    /// <summary>
    /// Every error response (exceptions, framework status codes and model validation) gets the same
    /// shape: <c>type</c> derived from <c>code</c>, <c>instance</c> and <c>correlationId</c>.
    /// </summary>
    public static IServiceCollection AddWizerProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var status = problem.Status ?? context.HttpContext.Response.StatusCode;

            if (problem.Extensions.TryGetValue("code", out var value) is false || value is not string code)
            {
                code = problem is HttpValidationProblemDetails
                    ? ErrorCodes.ValidationError
                    : ErrorCodes.DefaultFor(status);
                problem.Extensions["code"] = code;
            }

            problem.Type = ErrorCodes.TypeFor(code);
            problem.Instance = context.HttpContext.Request.Path;
            problem.Extensions.Remove("traceId");
            problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        });
        return services;
    }
}
