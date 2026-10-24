using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Classes.Entities;

namespace Wizer.Api.Modules.Classes.Configurations;

public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("school_classes", table => table.HasCheckConstraint(
            "ck_school_classes_join_code",
            $"join_code ~ '^[{SchoolClass.JoinCodeAlphabet}]{{{SchoolClass.JoinCodeLength}}}$'"));
        builder.HasKey(schoolClass => schoolClass.Id);
        builder.HasQueryFilter(schoolClass => schoolClass.DeletedAt == null);

        builder.Property(schoolClass => schoolClass.Name).HasMaxLength(120);
        builder.Property(schoolClass => schoolClass.Description).HasMaxLength(2000);
        builder.Property(schoolClass => schoolClass.Schedule).HasMaxLength(200);
        builder.Property(schoolClass => schoolClass.JoinCode).HasMaxLength(SchoolClass.JoinCodeLength).IsFixedLength();

        builder.HasOne<Institution>().WithMany().HasForeignKey(schoolClass => schoolClass.InstitutionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(schoolClass => schoolClass.JoinCode)
            .IsUnique()
            .HasFilter("archived_at IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("ux_school_classes_join_code");
        builder.HasIndex(schoolClass => schoolClass.InstitutionId).HasDatabaseName("ix_school_classes_institution_id");
    }
}
