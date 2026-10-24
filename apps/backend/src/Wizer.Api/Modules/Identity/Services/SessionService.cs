using Microsoft.AspNetCore.Identity;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Modules.Identity.Mappers;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Services;

/// <summary>Login, refresh with rotation and reuse detection, logout and the current user (AUTH-02, 04, 06, 10).</summary>
public sealed class SessionService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ISessionIssuer sessions,
    IVerificationCodeSender codeSender,
    IAuthCookieWriter cookies,
    LoginAttemptLimiter limiter,
    IPasswordHasher<User> passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ISessionService
{
    // Verified against when the account does not exist, so a wrong email costs as much as a wrong password.
    private static readonly Lazy<string> DummyHash = new(() => new PasswordHasher<User>().HashPassword(null!, "not-a-real-password-1"));

    public async Task<MeResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(request.Email);
        limiter.Reserve(email);

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user?.PasswordHash is null)
        {
            passwordHasher.VerifyHashedPassword(null!, DummyHash.Value, request.Password);
            throw InvalidCredentials();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw InvalidCredentials();
        }

        limiter.Reset(email);

        if (user.IsActive is false)
        {
            throw new ForbiddenException("This account is inactive.", IdentityErrorCodes.AccountInactive);
        }

        if (user.EmailVerifiedAt is null)
        {
            await codeSender.SendAsync(user, VerificationPurpose.EmailVerification, cancellationToken);
            throw new ForbiddenException("The email is not verified. A new code was sent.", IdentityErrorCodes.EmailNotVerified);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        var session = sessions.Issue(user, request.RememberMe);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        cookies.WriteSession(session.AccessToken, session.RefreshToken, session.RefreshExpiresAt);
        return user.ToMe();
    }

    public async Task<MeResponse?> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var stored = string.IsNullOrEmpty(refreshToken)
            ? null
            : await refreshTokens.FindByHashAsync(SecretHash.Of(refreshToken), cancellationToken);
        if (stored is null || stored.ExpiresAt <= now)
        {
            cookies.Clear();
            return null;
        }

        if (stored.RevokedAt is not null)
        {
            // A rotated token came back: someone else may hold the family, so nobody keeps it.
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            cookies.Clear();
            return null;
        }

        var user = await users.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is not { IsActive: true, EmailVerifiedAt: not null })
        {
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            cookies.Clear();
            return null;
        }

        IssuedSession session;
        bool rotated;
        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            session = sessions.Rotate(user, stored);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            rotated = await refreshTokens.MarkRotatedAsync(stored.Id, session.RefreshTokenId, now, cancellationToken);
            if (rotated)
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        if (rotated is false)
        {
            // Another request rotated the same token concurrently: treated as reuse.
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            cookies.Clear();
            return null;
        }

        cookies.WriteSession(session.AccessToken, session.RefreshToken, session.RefreshExpiresAt);
        return user.ToMe();
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken) is false)
        {
            var stored = await refreshTokens.FindByHashAsync(SecretHash.Of(refreshToken), cancellationToken);
            if (stored is not null)
            {
                await refreshTokens.RevokeAsync(stored.Id, clock.UtcNow, cancellationToken);
            }
        }

        cookies.Clear();
    }

    public async Task<MeResponse?> GetMeAsync(CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(currentUser.Id, cancellationToken);
        return user is { IsActive: true } ? user.ToMe() : null;
    }

    private static UnauthorizedException InvalidCredentials() =>
        new("Email or password is incorrect.", IdentityErrorCodes.InvalidCredentials);
}
