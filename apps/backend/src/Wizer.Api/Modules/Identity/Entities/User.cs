using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Identity.Entities;

/// <summary>
/// A platform account. Users are deactivated (<see cref="IsActive"/>), never deleted, so their
/// messages, submissions and files survive (BR-ACC-04).
/// </summary>
public class User : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; set; }

    public Guid InstitutionId { get; set; }

    /// <summary>Null only for the first administrator, seeded before any campus exists.</summary>
    public Guid? CampusId { get; set; }

    public required string FullName { get; set; }

    /// <summary>Stored normalized: trimmed and lowercase. Unique.</summary>
    public required string Email { get; set; }

    /// <summary>Null for Google-only accounts and for invited users who have not set a password yet.</summary>
    public string? PasswordHash { get; set; }

    public PlatformRole PlatformRole { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Null until the email is verified; login is refused while it is null (BR-ACC-03).</summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }

    /// <summary>The current avatar in <c>files</c> (context <c>avatar</c>).</summary>
    public Guid? AvatarFileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
