using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Chat.Configurations;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("message_reactions", table => table.HasEnumCheck<ReactionType>("reaction"));
        builder.HasKey(reaction => reaction.Id);
        builder.Property(reaction => reaction.Reaction).HasSnakeCaseConversion();

        builder.HasOne<ChatMessage>().WithMany().HasForeignKey(reaction => reaction.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(reaction => reaction.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reaction => new { reaction.MessageId, reaction.UserId, reaction.Reaction })
            .IsUnique()
            .HasDatabaseName("ux_message_reactions_message_id_user_id_reaction");
    }
}
