using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Assignments.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments", table =>
        {
            table.HasEnumCheck<AssignmentMode>("mode");
            table.HasEnumCheck<GradingType>("grading_type");
            table.HasEnumCheck<AssignmentStatus>("status");
            table.HasCheckConstraint(
                "ck_assignments_max_points",
                "(grading_type = 'points' AND max_points BETWEEN 1 AND 100)"
                + " OR (grading_type = 'pass_fail' AND max_points IS NULL)");
            table.HasCheckConstraint(
                "ck_assignments_late_until",
                "late_until IS NULL OR (allow_late_submissions AND late_until > due_at)");
        });
        builder.HasKey(assignment => assignment.Id);
        builder.HasQueryFilter(assignment => assignment.DeletedAt == null);

        builder.Property(assignment => assignment.Title).HasMaxLength(200);
        builder.Property(assignment => assignment.Instructions).HasMaxLength(10000);
        builder.Property(assignment => assignment.Mode).HasSnakeCaseConversion();
        builder.Property(assignment => assignment.GradingType).HasSnakeCaseConversion();
        builder.Property(assignment => assignment.Status).HasSnakeCaseConversion();
        builder.Property(assignment => assignment.MaxPoints).HasPrecision(5, 2);
        builder.Property(assignment => assignment.RowVersion).IsRowVersion();

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(assignment => assignment.ClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(assignment => assignment.CreatedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => new { assignment.ClassId, assignment.DueAt }).HasDatabaseName("ix_assignments_class_id_due_at");
    }
}
