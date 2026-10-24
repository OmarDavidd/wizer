using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IPasswordRecoveryService
{
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
