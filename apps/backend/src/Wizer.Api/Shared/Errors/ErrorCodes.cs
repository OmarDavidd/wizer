namespace Wizer.Api.Shared.Errors;

/// <summary>
/// General error codes shared by every module. Module-specific codes live next to the module
/// and are documented in .ia/errors.md.
/// </summary>
public static class ErrorCodes
{
    public const string BadRequest = "BAD_REQUEST";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string Conflict = "CONFLICT";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

    private const string TypeBaseUri = "https://wizer.app/errors/";

    public static string DefaultFor(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => Unauthorized,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => ResourceNotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status409Conflict => Conflict,
        StatusCodes.Status413PayloadTooLarge => PayloadTooLarge,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        StatusCodes.Status429TooManyRequests => RateLimited,
        StatusCodes.Status503ServiceUnavailable => ServiceUnavailable,
        >= 400 and < 500 => BadRequest,
        _ => InternalError,
    };

    public static string TypeFor(string code) =>
        TypeBaseUri + code.ToLowerInvariant().Replace('_', '-');
}
