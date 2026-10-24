using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IRegistrationService registration) : ControllerBase
{
    /// <summary>Always 202 without a body, whether or not the email exists.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await registration.RegisterAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>Verifies the email and signs in: 200 with the session cookies.</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeResponse>> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken) =>
        await registration.VerifyEmailAsync(request, cancellationToken);

    /// <summary>Always 202, whether or not the email exists.</summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await registration.ResendVerificationAsync(request, cancellationToken);
        return Accepted();
    }
}
