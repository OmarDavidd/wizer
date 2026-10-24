using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Shared.Auth;

namespace Wizer.IntegrationTests.Infrastructure.Authentication;

/// <summary>Test-only endpoints to exercise authentication, role policies and request validation.</summary>
[ApiController]
[Route("__test/auth")]
public sealed class AuthProbeTestController(ICurrentUser currentUser) : ControllerBase
{
    // No attribute: protected by the global [Authorize].
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { currentUser.Id, Role = currentUser.Role.ToString(), currentUser.InstitutionId });

    [HttpGet("admin")]
    [Authorize(Policy = AuthPolicies.Admin)]
    public IActionResult AdminOnly() => Ok();

    [HttpGet("anonymous")]
    [AllowAnonymous]
    public IActionResult Anonymous() => Ok();

    [HttpPost("validated")]
    [AllowAnonymous]
    public IActionResult Validated(ProbeRequest request) => Ok(request);
}

public sealed record ProbeRequest(string Name);

public sealed class ProbeRequestValidator : AbstractValidator<ProbeRequest>
{
    public ProbeRequestValidator() => RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
}
