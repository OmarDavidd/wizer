using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Chat.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", table => table.HasCheckConstraint(
            "ck_chat_messages_content",
            $"char_length(content) BETWEEN 1 AND {ChatMessage.MaxContentLength}"));
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Content).HasMaxLength(ChatMessage.MaxContentLength);

        builder.HasOne<ChatChannel>().WithMany().HasForeignKey(message => message.ChannelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(message => message.SenderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(message => message.DeletedById).OnDelete(DeleteBehavior.Restrict);

        // BR-CHAT-08: a resend with the same client id is stored once.
        builder.HasIndex(message => new { message.SenderId, message.ClientMessageId })
            .IsUnique()
            .HasDatabaseName("ux_chat_messages_sender_id_client_message_id");
        // Cursor pagination, newest first.
        builder.HasIndex(message => new { message.ChannelId, message.CreatedAt, message.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("ix_chat_messages_channel_id_created_at_id");
    }
}
