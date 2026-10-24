using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Chat.Entities;

public enum ChatChannelType
{
    /// <summary>The general channel of a class (BR-CHAT-01). One per class.</summary>
    Class,

    /// <summary>A team's private channel (BR-CHAT-01). One per team.</summary>
    Team,

    /// <summary>The questions thread of an assignment (CHAT-10). One per assignment.</summary>
    Assignment,
}

/// <summary>A channel always belongs to a class; <see cref="TeamId"/> or <see cref="AssignmentId"/> is set by type.</summary>
public class ChatChannel : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public ChatChannelType Type { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? AssignmentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
