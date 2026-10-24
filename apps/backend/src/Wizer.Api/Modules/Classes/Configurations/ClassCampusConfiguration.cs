using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Classes.Entities;

namespace Wizer.Api.Modules.Classes.Configurations;

public sealed class ClassCampusConfiguration : IEntityTypeConfiguration<ClassCampus>
{
    public void Configure(EntityTypeBuilder<ClassCampus> builder)
    {
        builder.ToTable("class_campuses");
        builder.HasKey(classCampus => new { classCampus.ClassId, classCampus.CampusId });

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(classCampus => classCampus.ClassId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Campus>().WithMany().HasForeignKey(classCampus => classCampus.CampusId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(classCampus => classCampus.CampusId).HasDatabaseName("ix_class_campuses_campus_id");
    }
}
