using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IRegistrationService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Verifies the email and signs the user in (writes the session cookies).</summary>
    Task<MeResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken);
}
