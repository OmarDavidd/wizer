using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Identity.Entities;

/// <summary>
/// Single-use link an administrator sends to a teacher or administrator to set their password.
/// Only the SHA-256 hash is stored. Expires in 72 h; resending revokes the previous one.
/// </summary>
public class InvitationToken : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public Guid InvitedById { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
