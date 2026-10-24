using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Chat.Entities;

public enum ChatAccessStatus
{
    Pending,
    Approved,
    Rejected,
    Revoked,
}

/// <summary>
/// A teacher's request to read and write in a team's chat (BR-CHAT-10 to 15). The first member who answers
/// resolves it; any member can revoke an approved one. At most one pending request per team.
/// </summary>
public class ChatAccessGrant : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public Guid TeacherId { get; set; }

    public ChatAccessStatus Status { get; set; }

    public Guid? ResolvedById { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public Guid? RevokedById { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
