namespace Wizer.Api.Shared.Exceptions;

public sealed class BusinessRuleException(string code, string detail)
    : AppException(StatusCodes.Status400BadRequest, code, detail);
