using FluentValidation;
using FluentValidation.Results;
using Wizer.Api.Infrastructure.Authentication;
using Wizer.Api.Infrastructure.Authentication.Google;
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
/// Login with Google (AUTH-08). A known Google account signs in; an existing local account is linked only
/// if its email was already verified; otherwise a verified student without password is created, in the
/// campus the client sends (BR-ACC-01, BR-ACC-05).
/// </summary>
public sealed class GoogleSignInService(
    IGoogleTokenValidator google,
    IUserRepository users,
    IExternalLoginRepository externalLogins,
    ICampusModuleService campuses,
    ISessionIssuer sessions,
    IAuthCookieWriter cookies,
    IUnitOfWork unitOfWork,
    IIdGenerator ids,
    IClock clock) : IGoogleSignInService
{
    public async Task<MeResponse> SignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var identity = await google.ValidateAsync(request.IdToken, cancellationToken)
            ?? throw new UnauthorizedException("The Google credential is not valid.", IdentityErrorCodes.GoogleTokenInvalid);
        var email = EmailAddress.Normalize(identity.Email);

        var linkedUserId = await externalLogins.FindUserIdAsync(ExternalLoginProvider.Google, identity.Subject, cancellationToken);
        var user = linkedUserId is not null
            ? await users.FindByIdAsync(linkedUserId.Value, cancellationToken)
            : await users.FindByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            user = await CreateStudentAsync(identity, email, request.CampusId, cancellationToken);
            Link(user, identity, email);
        }
        else
        {
            if (user.IsActive is false)
            {
                throw new ForbiddenException("This account is inactive.", IdentityErrorCodes.AccountInactive);
            }

            if (linkedUserId is null)
            {
                // Linking proves nothing about an inbox nobody verified: the owner signs in with the password first.
                if (user.EmailVerifiedAt is null)
                {
                    throw new ConflictException(
                        "An account with this email exists. Sign in with your password first.",
                        IdentityErrorCodes.EmailAlreadyExists);
                }

                Link(user, identity, email);
            }
        }

        var session = sessions.Issue(user, request.RememberMe);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        cookies.WriteSession(session.AccessToken, session.RefreshToken, session.RefreshExpiresAt);
        return user.ToMe();
    }

    private async Task<User> CreateStudentAsync(GoogleIdentity identity, string email, Guid? campusId, CancellationToken cancellationToken)
    {
        if (campusId is null)
        {
            throw new BusinessRuleException(IdentityErrorCodes.CampusRequired, "Choose a campus to create the account.");
        }

        var campus = await campuses.FindActiveAsync(campusId.Value, cancellationToken)
            ?? throw new ValidationException([new ValidationFailure("CampusId", "The campus does not exist or is not active.")]);

        var user = new User
        {
            Id = ids.NewId(),
            InstitutionId = campus.InstitutionId,
            CampusId = campus.Id,
            FullName = Truncate(identity.Name ?? email[..email.IndexOf('@')], 120),
            Email = email,
            PlatformRole = PlatformRole.Student,
            IsActive = true,
            EmailVerifiedAt = clock.UtcNow,
        };
        users.Add(user);
        return user;
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    private void Link(User user, GoogleIdentity identity, string email) =>
        externalLogins.Add(new ExternalLogin
        {
            Id = ids.NewId(),
            UserId = user.Id,
            Provider = ExternalLoginProvider.Google,
            ProviderKey = identity.Subject,
            Email = email,
        });
}
