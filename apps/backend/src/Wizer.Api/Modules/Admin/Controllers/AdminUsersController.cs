using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wizer.Api.Modules.Admin.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Modules.Admin.Controllers;

/// <summary>User administration for platform administrators (ADM-02, AUTH-09).</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminUsersController(IIdentityModuleService identity) : ControllerBase
{
    /// <summary>Invites a teacher or administrator: 201 with the new (inactive) account.</summary>
    [HttpPost]
    [ProducesResponseType<MeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeResponse>> Invite(InviteUserRequest request, CancellationToken cancellationToken)
    {
        var role = request.Role == "admin" ? PlatformRole.Admin : PlatformRole.Teacher;
        var invited = await identity.InviteAsync(
            new InviteUserCommand(request.FullName, request.Email, role, request.CampusId),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, invited);
    }

    /// <summary>Sends a new activation link; the previous one stops working.</summary>
    [HttpPost("{userId:guid}/resend-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInvitation(Guid userId, CancellationToken cancellationToken)
    {
        await identity.ResendInvitationAsync(userId, cancellationToken);
        return NoContent();
    }
}
