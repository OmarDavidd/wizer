using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Teams.Entities;

namespace Wizer.Api.Modules.Chat.Configurations;

public sealed class ChatAccessGrantConfiguration : IEntityTypeConfiguration<ChatAccessGrant>
{
    public void Configure(EntityTypeBuilder<ChatAccessGrant> builder)
    {
        builder.ToTable("chat_access_grants", table => table.HasEnumCheck<ChatAccessStatus>("status"));
        builder.HasKey(grant => grant.Id);
        builder.Property(grant => grant.Status).HasSnakeCaseConversion();

        builder.HasOne<Team>().WithMany().HasForeignKey(grant => grant.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(grant => grant.TeacherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(grant => grant.ResolvedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(grant => grant.RevokedById).OnDelete(DeleteBehavior.Restrict);

        // BR-CHAT-15: at most one pending request per team.
        builder.HasIndex(grant => grant.TeamId)
            .IsUnique()
            .HasFilter("status = 'pending'")
            .HasDatabaseName("ux_chat_access_grants_team_id_pending");
        builder.HasIndex(grant => new { grant.TeamId, grant.Status }).HasDatabaseName("ix_chat_access_grants_team_id_status");
    }
}
