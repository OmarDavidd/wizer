namespace Wizer.Api.Shared.Auth;

/// <summary>The only source of "who is making the request". Resolved from the JWT claims.</summary>
public interface ICurrentUser
{
    Guid Id { get; }

    PlatformRole Role { get; }

    Guid InstitutionId { get; }
}
