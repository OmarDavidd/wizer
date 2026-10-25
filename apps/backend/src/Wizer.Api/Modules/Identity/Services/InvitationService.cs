using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Campuses.Interfaces;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.DTOs;
using Wizer.Api.Modules.Identity.DTOs.Requests;
using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Modules.Identity.Mappers;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Configuration;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Services;

/// <summary>
/// Teacher and administrator accounts by invitation (AUTH-09, BR-ACC-02): an inactive account without
/// password and a single-use link (only its SHA-256 is stored) valid for 72 hours.
/// </summary>
public sealed class InvitationService(
    IUserRepository users,
    IInvitationTokenRepository invitations,
    ICampusModuleService campuses,
    ISessionIssuer sessions,
    IAuthCookieWriter cookies,
    IEmailQueue emails,
    IPasswordHasher<User> passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IOptions<AppOptions> appOptions,
    IIdGenerator ids,
    IClock clock) : IInvitationService, IIdentityModuleService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(72);

    public async Task<MeResponse> InviteAsync(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var campus = await campuses.FindActiveAsync(command.CampusId, cancellationToken);
        if (campus is null || campus.InstitutionId != currentUser.InstitutionId)
        {
            throw new ValidationException([new ValidationFailure("CampusId", "The campus does not exist or is not active.")]);
        }

        var email = EmailAddress.Normalize(command.Email);
        if (await users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            throw EmailAlreadyExists();
        }

        var user = new User
        {
            Id = ids.NewId(),
            InstitutionId = campus.InstitutionId,
            CampusId = campus.Id,
            FullName = command.FullName.Trim(),
            Email = email,
            PlatformRole = command.Role,
            IsActive = false,
        };
        users.Add(user);
        var token = AddInvitation(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            throw EmailAlreadyExists();
        }

        emails.Enqueue(IdentityEmails.Invitation(user, ActivationLink(token)));
        return user.ToMe();
    }

    public async Task ResendInvitationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.InstitutionId != currentUser.InstitutionId)
        {
            throw new NotFoundException("The user was not found.");
        }

        // Only a teacher or administrator invited and never activated: an inactive account without password
        // can also be a deactivated Google student, who must never get an activation link.
        var pendingInvitation = user is { IsActive: false, PasswordHash: null, EmailVerifiedAt: null }
            && user.PlatformRole is PlatformRole.Teacher or PlatformRole.Admin;
        if (!pendingInvitation)
        {
            throw new ConflictException("This account has no pending invitation.");
        }

        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            await invitations.RevokePendingAsync(user.Id, clock.UtcNow, cancellationToken);
            var token = AddInvitation(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            emails.Enqueue(IdentityEmails.Invitation(user, ActivationLink(token)));
        }
    }

    public async Task<MeResponse> SetPasswordAsync(SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var invalidToken = new BusinessRuleException(
            IdentityErrorCodes.InvalidOrExpiredToken,
            "The invitation link is invalid, expired or already used.");
        var now = clock.UtcNow;

        var invitation = await invitations.FindUsableByHashAsync(SecretHash.Of(request.Token), now, cancellationToken)
            ?? throw invalidToken;
        var user = await users.FindByIdAsync(invitation.UserId, cancellationToken) ?? throw invalidToken;

        if (string.Equals(request.Password.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException([new ValidationFailure(nameof(request.Password), "Password must not be the email.")]);
        }

        IssuedSession session;
        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            if (await invitations.ConsumeAsync(invitation.Id, now, cancellationToken) is false)
            {
                throw invalidToken;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.IsActive = true;
            user.EmailVerifiedAt ??= now;
            session = sessions.Issue(user, rememberMe: false);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        cookies.WriteSession(session.AccessToken, session.RefreshToken, session.RefreshExpiresAt);
        return user.ToMe();
    }

    private string AddInvitation(User user)
    {
        var token = SecretHash.NewToken();
        invitations.Add(new InvitationToken
        {
            Id = ids.NewId(),
            UserId = user.Id,
            TokenHash = SecretHash.Of(token),
            InvitedById = currentUser.Id,
            ExpiresAt = clock.UtcNow + Lifetime,
        });
        return token;
    }

    private string ActivationLink(string token) =>
        $"{appOptions.Value.PublicUrl.TrimEnd('/')}/activar?token={Uri.EscapeDataString(token)}";

    private static ConflictException EmailAlreadyExists() =>
        new("An account with this email already exists.", IdentityErrorCodes.EmailAlreadyExists);
}
