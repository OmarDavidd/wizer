using Wizer.Api.Modules.Identity.DTOs;
using Wizer.Api.Modules.Identity.DTOs.Responses;

namespace Wizer.Api.Modules.Identity.Interfaces;

/// <summary>The Identity module as seen by other modules (Admin).</summary>
public interface IIdentityModuleService
{
    /// <summary>Creates an inactive account without password and emails a single-use 72 h activation link.</summary>
    Task<MeResponse> InviteAsync(InviteUserCommand command, CancellationToken cancellationToken);

    /// <summary>Revokes the pending link and emails a new one.</summary>
    Task ResendInvitationAsync(Guid userId, CancellationToken cancellationToken);
}
