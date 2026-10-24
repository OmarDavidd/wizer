using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Teams.Entities;

namespace Wizer.Api.Modules.Teams.Configurations;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");
        builder.HasKey(member => member.Id);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(member => new { member.TeamId, member.ClassId })
            .HasPrincipalKey(team => new { team.Id, team.ClassId })
            .HasConstraintName("fk_team_members_teams_team_id_class_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(member => new { member.TeamId, member.UserId })
            .IsUnique()
            .HasDatabaseName("ux_team_members_team_id_user_id");
        // BR-TEAM-02: one team per student and class.
        builder.HasIndex(member => new { member.ClassId, member.UserId })
            .IsUnique()
            .HasDatabaseName("ux_team_members_class_id_user_id");
        builder.HasIndex(member => member.UserId).HasDatabaseName("ix_team_members_user_id");
    }
}
