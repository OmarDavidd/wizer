using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Teams.Entities;

namespace Wizer.Api.Modules.Chat.Configurations;

public sealed class ChatChannelConfiguration : IEntityTypeConfiguration<ChatChannel>
{
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        builder.ToTable("chat_channels", table =>
        {
            table.HasEnumCheck<ChatChannelType>("type");
            table.HasCheckConstraint(
                "ck_chat_channels_owner",
                "(type = 'class' AND team_id IS NULL AND assignment_id IS NULL)"
                + " OR (type = 'team' AND team_id IS NOT NULL AND assignment_id IS NULL)"
                + " OR (type = 'assignment' AND assignment_id IS NOT NULL AND team_id IS NULL)");
        });
        builder.HasKey(channel => channel.Id);
        builder.Property(channel => channel.Type).HasSnakeCaseConversion();

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(channel => channel.ClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Team>().WithMany().HasForeignKey(channel => channel.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Assignment>().WithMany().HasForeignKey(channel => channel.AssignmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(channel => channel.ClassId)
            .IsUnique()
            .HasFilter("type = 'class'")
            .HasDatabaseName("ux_chat_channels_class_id_general");
        builder.HasIndex(channel => channel.TeamId).IsUnique().HasDatabaseName("ux_chat_channels_team_id");
        builder.HasIndex(channel => channel.AssignmentId).IsUnique().HasDatabaseName("ux_chat_channels_assignment_id");
    }
}
