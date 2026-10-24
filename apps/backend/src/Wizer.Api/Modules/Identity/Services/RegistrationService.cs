using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Modules.Campuses.Interfaces;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Modules.Identity.Mappers;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Services;

/// <summary>
/// Student registration and email verification (AUTH-01, AUTH-05, AUTH-07). Register and resend answer
/// the same whether or not the email exists, so they never reveal which accounts there are.
/// </summary>
public sealed class RegistrationService(
    IUserRepository users,
    IVerificationCodeRepository codes,
    IVerificationCodeSender codeSender,
    ISessionIssuer sessions,
    IAuthCookieWriter cookies,
    ICampusModuleService campuses,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork,
    IIdGenerator ids,
    IClock clock) : IRegistrationService
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var campus = await campuses.FindActiveAsync(request.CampusId, cancellationToken)
            ?? throw new ValidationException([new ValidationFailure(nameof(request.CampusId), "The campus does not exist or is not active.")]);

        // Hashed even when the account exists, so both paths take the same time.
        var passwordHash = passwordHasher.HashPassword(null!, request.Password);
        var email = EmailAddress.Normalize(request.Email);

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = ids.NewId(),
                InstitutionId = campus.InstitutionId,
                CampusId = campus.Id,
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                PlatformRole = PlatformRole.Student,
                IsActive = true,
            };
            users.Add(user);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The same email registered concurrently; that request sends the code.
                return;
            }
        }

        // An existing account keeps its password: whoever registered first must prove the inbox.
        if (user.EmailVerifiedAt is null && user.IsActive)
        {
            await codeSender.SendAsync(user, VerificationPurpose.EmailVerification, cancellationToken);
        }
    }

    public async Task<MeResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var invalidCode = new BusinessRuleException(
            IdentityErrorCodes.InvalidVerificationCode,
            "The verification code is invalid or expired.");
        var now = clock.UtcNow;

        var user = await users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken);
        if (user is null || user.EmailVerifiedAt is not null || user.IsActive is false)
        {
            throw invalidCode;
        }

        var code = await codes.ReserveAttemptAsync(
            user.Id, VerificationPurpose.EmailVerification, now, VerificationCodeSender.MaxAttempts, cancellationToken);
        if (code is null || SecretHash.Matches(code.CodeHash, request.Code) is false)
        {
            throw invalidCode;
        }

        if (user.PasswordHash is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new BusinessRuleException(
                IdentityErrorCodes.VerificationPasswordMismatch,
                "The code is valid but the password does not match this account.");
        }

        IssuedSession session;
        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            if (await codes.ConsumeAsync(code.Id, now, cancellationToken) is false)
            {
                throw invalidCode;
            }

            user.EmailVerifiedAt = now;
            session = sessions.Issue(user, rememberMe: false);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        cookies.WriteSession(session.AccessToken, session.RefreshToken, session.RefreshExpiresAt);
        return user.ToMe();
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken);
        if (user is { EmailVerifiedAt: null, IsActive: true })
        {
            await codeSender.SendAsync(user, VerificationPurpose.EmailVerification, cancellationToken);
        }
    }
}
