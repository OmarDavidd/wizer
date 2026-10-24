using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Identity.Entities;

/// <summary>
/// Only the SHA-256 hash of the opaque token is stored. Every refresh rotates the token: the old one is
/// revoked and points to its replacement. Reusing a rotated token revokes its whole <see cref="FamilyId"/>.
/// </summary>
public class RefreshToken : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>Hex SHA-256 of the token sent in the <c>wz_refresh</c> cookie.</summary>
    public required string TokenHash { get; set; }

    /// <summary>"Remember me": 30 days instead of 7.</summary>
    public bool IsPersistent { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
