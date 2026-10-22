using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Shared.Exceptions;

namespace Wizer.IntegrationTests.Shared;

/// <summary>Test-only endpoints that throw, to exercise the global exception handler end to end.</summary>
[ApiController]
[Route("__test/throw")]
public sealed class ThrowingTestController : ControllerBase
{
    public const string SecretMessage = "Host=db;Password=super-secret";

    [HttpGet("unexpected")]
    public IActionResult Unexpected() => throw new InvalidOperationException(SecretMessage);

    [HttpGet("business")]
    public IActionResult Business() => throw new BusinessRuleException("CLASS_ARCHIVED", "The class is archived.");

    [HttpGet("validation")]
    public IActionResult Validation() =>
        throw new ValidationException([new ValidationFailure("Name", "Name is required.")]);

    [HttpGet("payload-too-large")]
    public IActionResult PayloadTooLarge() =>
        throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
}
