using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Chat.Entities;

/// <summary>
/// A chat message of 1 to 4000 characters (BR-CHAT-02). Deleting is a soft delete that the UI shows as
/// "Mensaje eliminado" (BR-CHAT-04), so this entity has no global query filter: read queries keep deleted
/// rows and hide their content. <see cref="ClientMessageId"/> makes resends idempotent (BR-CHAT-08).
/// Attachments are <c>files</c> rows pointing to the message.
/// </summary>
public class ChatMessage : IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
{
    public const int MaxContentLength = 4000;

    public Guid Id { get; set; }

    public Guid ChannelId { get; set; }

    public Guid SenderId { get; set; }

    public Guid ClientMessageId { get; set; }

    public required string Content { get; set; }

    /// <summary>Set when the author edits the message; the UI shows "editado" (BR-CHAT-03).</summary>
    public DateTimeOffset? EditedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>The author or the class teacher (BR-CHAT-05).</summary>
    public Guid? DeletedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
