using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IInvitationService
{
    /// <summary>Sets the password, verifies the email, activates the account and signs in (writes the cookies).</summary>
    Task<MeResponse> SetPasswordAsync(SetPasswordRequest request, CancellationToken cancellationToken);
}
