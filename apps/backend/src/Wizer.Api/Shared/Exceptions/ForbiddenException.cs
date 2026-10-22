using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.Exceptions;

public sealed class ForbiddenException(string detail, string code = ErrorCodes.Forbidden)
    : AppException(StatusCodes.Status403Forbidden, code, detail);
