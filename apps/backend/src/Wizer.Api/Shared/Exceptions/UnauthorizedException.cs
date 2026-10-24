using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.Exceptions;

public sealed class UnauthorizedException(string detail, string code = ErrorCodes.Unauthorized)
    : AppException(StatusCodes.Status401Unauthorized, code, detail);
