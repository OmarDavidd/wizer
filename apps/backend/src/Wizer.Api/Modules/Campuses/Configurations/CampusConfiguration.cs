using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Campuses.Entities;

namespace Wizer.Api.Modules.Campuses.Configurations;

public sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campuses");
        builder.HasKey(campus => campus.Id);
        builder.Property(campus => campus.Name).HasMaxLength(120);

        builder.HasOne<Institution>().WithMany().HasForeignKey(campus => campus.InstitutionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(campus => new { campus.InstitutionId, campus.Name }).IsUnique().HasDatabaseName("ux_campuses_institution_id_name");
    }
}
