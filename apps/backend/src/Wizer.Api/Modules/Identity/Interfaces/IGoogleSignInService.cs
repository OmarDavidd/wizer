using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IGoogleSignInService
{
    /// <summary>Signs in with Google (linking or creating the account) and writes the session cookies.</summary>
    Task<MeResponse> SignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken);
}
