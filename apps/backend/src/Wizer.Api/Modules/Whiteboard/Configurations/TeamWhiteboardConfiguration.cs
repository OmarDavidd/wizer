using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Teams.Entities;
using Wizer.Api.Modules.Whiteboard.Entities;

namespace Wizer.Api.Modules.Whiteboard.Configurations;

public sealed class TeamWhiteboardConfiguration : IEntityTypeConfiguration<TeamWhiteboard>
{
    public void Configure(EntityTypeBuilder<TeamWhiteboard> builder)
    {
        builder.ToTable("whiteboards", table => table.HasCheckConstraint("ck_whiteboards_version", "version >= 0"));
        builder.HasKey(whiteboard => whiteboard.Id);

        builder.Property(whiteboard => whiteboard.Scene).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(whiteboard => whiteboard.RowVersion).IsRowVersion();

        builder.HasOne<Team>().WithOne().HasForeignKey<TeamWhiteboard>(whiteboard => whiteboard.TeamId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(whiteboard => whiteboard.TeamId).IsUnique().HasDatabaseName("ux_whiteboards_team_id");
    }
}
