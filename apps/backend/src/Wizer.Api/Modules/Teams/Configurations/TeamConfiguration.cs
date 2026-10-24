using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Teams.Entities;

namespace Wizer.Api.Modules.Teams.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams", table => table.HasCheckConstraint(
            "ck_teams_max_size",
            $"max_size BETWEEN {Team.MinSize} AND {Team.MaxSizeLimit}"));
        builder.HasKey(team => team.Id);
        // Target of team_members' composite foreign key, so a member's class_id always matches its team.
        builder.HasAlternateKey(team => new { team.Id, team.ClassId }).HasName("ak_teams_id_class_id");
        builder.HasQueryFilter(team => team.DeletedAt == null);

        builder.Property(team => team.Name).HasMaxLength(80);
        builder.Property(team => team.RowVersion).IsRowVersion();

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(team => team.ClassId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(team => new { team.ClassId, team.Name })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_teams_class_id_name");
    }
}
