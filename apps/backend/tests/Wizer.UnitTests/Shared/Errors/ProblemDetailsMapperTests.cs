using FluentValidation;
using FluentValidation.Results;
using Wizer.Api.Shared.Errors;
using Wizer.Api.Shared.Exceptions;

namespace Wizer.UnitTests.Shared.Errors;

public class ProblemDetailsMapperTests
{
    [Fact]
    public void Map_NotFoundException_Returns404WithDefaultCode()
    {
        var problem = ProblemDetailsMapper.Map(new NotFoundException("Class was not found."));

        Assert.Equal(404, problem.Status);
        Assert.Equal(ErrorCodes.ResourceNotFound, problem.Extensions["code"]);
        Assert.Equal("Class was not found.", problem.Detail);
    }

    [Fact]
    public void Map_NotFoundExceptionWithCode_KeepsSpecificCode()
    {
        var problem = ProblemDetailsMapper.Map(new NotFoundException("Class was not found.", "CLASS_NOT_FOUND"));

        Assert.Equal(404, problem.Status);
        Assert.Equal("CLASS_NOT_FOUND", problem.Extensions["code"]);
        Assert.Equal("https://wizer.app/errors/class-not-found", problem.Type);
    }

    [Fact]
    public void Map_ForbiddenException_Returns403()
    {
        var problem = ProblemDetailsMapper.Map(new ForbiddenException("Not allowed."));

        Assert.Equal(403, problem.Status);
        Assert.Equal(ErrorCodes.Forbidden, problem.Extensions["code"]);
    }

    [Fact]
    public void Map_ConflictException_Returns409()
    {
        var problem = ProblemDetailsMapper.Map(new ConflictException("Already exists."));

        Assert.Equal(409, problem.Status);
        Assert.Equal(ErrorCodes.Conflict, problem.Extensions["code"]);
    }

    [Fact]
    public void Map_BusinessRuleException_Returns400WithItsCode()
    {
        var problem = ProblemDetailsMapper.Map(new BusinessRuleException("CLASS_ARCHIVED", "The class is archived."));

        Assert.Equal(400, problem.Status);
        Assert.Equal("CLASS_ARCHIVED", problem.Extensions["code"]);
        Assert.Equal("The class is archived.", problem.Detail);
    }

    [Fact]
    public void Map_ValidationException_Returns400WithCamelCaseFieldErrors()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Name", "Name is too short."),
            new ValidationFailure("StartDate", "Start date is required."),
        ]);

        var problem = ProblemDetailsMapper.Map(exception);

        Assert.Equal(400, problem.Status);
        Assert.Equal(ErrorCodes.ValidationError, problem.Extensions["code"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(["Name is required.", "Name is too short."], errors["name"]);
        Assert.Equal(["Start date is required."], errors["startDate"]);
    }

    [Fact]
    public void Map_UnexpectedException_Returns500WithoutInternalDetails()
    {
        var problem = ProblemDetailsMapper.Map(new InvalidOperationException("Secret connection string"));

        Assert.Equal(500, problem.Status);
        Assert.Equal(ErrorCodes.InternalError, problem.Extensions["code"]);
        Assert.DoesNotContain("Secret", problem.Detail);
    }

    [Theory]
    [InlineData(400, "BAD_REQUEST")]
    [InlineData(401, "UNAUTHORIZED")]
    [InlineData(403, "FORBIDDEN")]
    [InlineData(404, "RESOURCE_NOT_FOUND")]
    [InlineData(405, "METHOD_NOT_ALLOWED")]
    [InlineData(409, "CONFLICT")]
    [InlineData(413, "PAYLOAD_TOO_LARGE")]
    [InlineData(415, "UNSUPPORTED_MEDIA_TYPE")]
    [InlineData(429, "RATE_LIMITED")]
    [InlineData(418, "BAD_REQUEST")]
    [InlineData(500, "INTERNAL_ERROR")]
    [InlineData(503, "SERVICE_UNAVAILABLE")]
    public void DefaultCodeFor_Status_ReturnsGeneralCode(int status, string expected)
    {
        Assert.Equal(expected, ErrorCodes.DefaultFor(status));
    }

    [Fact]
    public void TypeFor_Code_DerivesKebabCaseUri()
    {
        Assert.Equal("https://wizer.app/errors/validation-error", ErrorCodes.TypeFor("VALIDATION_ERROR"));
    }
}
