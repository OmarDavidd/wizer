namespace Wizer.Api.Shared.Exceptions;

/// <summary>
/// Base class for expected errors. The global exception handler turns them into ProblemDetails
/// with their status and code; their message is safe to show to the client.
/// </summary>
public abstract class AppException(int status, string code, string detail) : Exception(detail)
{
    public int Status { get; } = status;

    public string Code { get; } = code;
}
