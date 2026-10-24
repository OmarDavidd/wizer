using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Shared.Exceptions;

namespace Wizer.Api.Shared.Errors;

public static class ProblemDetailsMapper
{
    public static ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validation => FromValidation(validation),
        AppException app => Create(app.Status, app.Code, app.Message),
        BadHttpRequestException badRequest => Create(
            badRequest.StatusCode,
            ErrorCodes.DefaultFor(badRequest.StatusCode),
            "The request could not be processed."),
        _ => Create(StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, "An unexpected error occurred."),
    };

    private static ProblemDetails FromValidation(ValidationException exception)
    {
        var problem = Create(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "One or more fields are invalid.");
        problem.Title = "Validation failed";
        problem.Extensions["errors"] = exception.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
        return problem;
    }

    private static ProblemDetails Create(int status, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Type = ErrorCodes.TypeFor(code),
            Detail = detail,
        };
        problem.Extensions["code"] = code;
        return problem;
    }
}
