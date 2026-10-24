using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Chat.Entities;

/// <summary>An <c>@</c> mention of a channel member; each one notifies the mentioned user (BR-CHAT-07).</summary>
public class MessageMention : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid MentionedUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
