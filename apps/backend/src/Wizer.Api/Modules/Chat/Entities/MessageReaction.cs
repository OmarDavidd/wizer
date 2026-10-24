using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Chat.Entities;

/// <summary>The fixed set of reactions (BR-CHAT-06): 👍 ❤️ 😂 😮 😢 🎉.</summary>
public enum ReactionType
{
    ThumbsUp,
    Heart,
    Laugh,
    Surprised,
    Sad,
    Party,
}

/// <summary>One reaction of each type per user and message (BR-CHAT-06).</summary>
public class MessageReaction : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid UserId { get; set; }

    public ReactionType Reaction { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
