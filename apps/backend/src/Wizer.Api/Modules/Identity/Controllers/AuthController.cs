using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Shared.RateLimiting;

namespace Wizer.Api.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IRegistrationService registration,
    ISessionService sessions,
    IPasswordRecoveryService recovery,
    IGoogleSignInService google) : ControllerBase
{
    /// <summary>Always 202 without a body, whether or not the email exists.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AnonymousEmail)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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
    [EnableRateLimiting(RateLimitPolicies.AnonymousEmail)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await registration.ResendVerificationAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>200 with the session cookies; 403 EMAIL_NOT_VERIFIED sends a new code.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MeResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        await sessions.LoginAsync(request, cancellationToken);

    /// <summary>Rotates the <c>wz_refresh</c> cookie and issues a new access cookie; 401 clears both.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Refresh(CancellationToken cancellationToken)
    {
        var me = await sessions.RefreshAsync(Request.Cookies[AuthCookies.Refresh], cancellationToken);
        return me is null ? Unauthorized() : me;
    }

    /// <summary>Anonymous so it also works with an expired access cookie.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sessions.LogoutAsync(Request.Cookies[AuthCookies.Refresh], cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var me = await sessions.GetMeAsync(cancellationToken);
        return me is null ? Unauthorized() : me;
    }

    /// <summary>Always 202, whether or not the account exists; the code is emailed in the background.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AnonymousEmail)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await recovery.ForgotPasswordAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>204 and every session is revoked; it does not sign in.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await recovery.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Signs in with a Google ID token: 200 with the session cookies. A new account needs <c>campusId</c>
    /// (400 CAMPUS_REQUIRED otherwise); an unverified local account with the same email is 409.
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeResponse>> Google(GoogleSignInRequest request, CancellationToken cancellationToken) =>
        await google.SignInAsync(request, cancellationToken);
}
