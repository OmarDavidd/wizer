using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.Exceptions;

/// <summary>429 with a <c>Retry-After</c> header, set by the global exception handler.</summary>
public sealed class TooManyRequestsException(string detail, string code, TimeSpan retryAfter)
    : AppException(StatusCodes.Status429TooManyRequests, code, detail)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
