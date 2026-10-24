using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Shared.Exceptions;

namespace Wizer.IntegrationTests.Shared;

/// <summary>Test-only endpoints that throw, to exercise the global exception handler end to end.</summary>
[ApiController]
[AllowAnonymous]
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

    [HttpGet("unauthorized")]
    public IActionResult NotAuthenticated() => throw new UnauthorizedException("Email or password is incorrect.", "INVALID_CREDENTIALS");

    [HttpGet("too-many-requests")]
    public IActionResult TooMany() =>
        throw new TooManyRequestsException("Too many attempts.", "TOO_MANY_LOGIN_ATTEMPTS", TimeSpan.FromSeconds(90.2));

    [HttpGet("payload-too-large")]
    public IActionResult PayloadTooLarge() =>
        throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
}
