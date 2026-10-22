using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.Exceptions;

public sealed class ConflictException(string detail, string code = ErrorCodes.Conflict)
    : AppException(StatusCodes.Status409Conflict, code, detail);
