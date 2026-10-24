using Microsoft.AspNetCore.Identity;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Services;

/// <summary>
/// Password recovery by code (AUTH-03). Forgot always answers the same and sends in the background; a
/// reset signs the user out everywhere, verifies the email and clears the login lockout, but does not sign in.
/// </summary>
public sealed class PasswordRecoveryService(
    IUserRepository users,
    IVerificationCodeRepository codes,
    IVerificationCodeSender codeSender,
    IRefreshTokenRepository refreshTokens,
    LoginAttemptLimiter limiter,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock) : IPasswordRecoveryService
{
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken);
        if (user is { IsActive: true })
        {
            await codeSender.SendAsync(user, VerificationPurpose.PasswordReset, cancellationToken);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var invalidCode = new BusinessRuleException(IdentityErrorCodes.InvalidResetCode, "The reset code is invalid or expired.");
        var now = clock.UtcNow;
        var email = EmailAddress.Normalize(request.Email);

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is not { IsActive: true })
        {
            throw invalidCode;
        }

        var code = await codes.ReserveAttemptAsync(
            user.Id, VerificationPurpose.PasswordReset, now, VerificationCodeSender.MaxAttempts, cancellationToken);
        if (code is null || SecretHash.Matches(code.CodeHash, request.Code) is false)
        {
            throw invalidCode;
        }

        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            if (await codes.ConsumeAsync(code.Id, now, cancellationToken) is false)
            {
                throw invalidCode;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
            user.EmailVerifiedAt ??= now;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        limiter.Reset(email);
    }
}
