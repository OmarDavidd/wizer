using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Chat.Configurations;

public sealed class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        builder.ToTable("message_mentions");
        builder.HasKey(mention => mention.Id);

        builder.HasOne<ChatMessage>().WithMany().HasForeignKey(mention => mention.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(mention => mention.MentionedUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mention => new { mention.MessageId, mention.MentionedUserId })
            .IsUnique()
            .HasDatabaseName("ux_message_mentions_message_id_mentioned_user_id");
        builder.HasIndex(mention => mention.MentionedUserId).HasDatabaseName("ix_message_mentions_mentioned_user_id");
    }
}
