using Wizer.Api.Shared.Errors;

namespace Wizer.Api.Shared.Exceptions;

public sealed class NotFoundException(string detail, string code = ErrorCodes.ResourceNotFound)
    : AppException(StatusCodes.Status404NotFound, code, detail);
