using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Submissions.Entities;

namespace Wizer.Api.Modules.Submissions.Configurations;

public sealed class SubmissionMemberGradeConfiguration : IEntityTypeConfiguration<SubmissionMemberGrade>
{
    public void Configure(EntityTypeBuilder<SubmissionMemberGrade> builder)
    {
        builder.ToTable("submission_member_grades", table =>
        {
            table.HasEnumCheck<GradeResult>("result");
            table.HasCheckConstraint(
                "ck_submission_member_grades_grade",
                "(points IS NOT NULL AND points >= 0 AND result IS NULL) OR (points IS NULL AND result IS NOT NULL)");
        });
        builder.HasKey(grade => grade.Id);

        builder.Property(grade => grade.Result).HasSnakeCaseConversion();
        builder.Property(grade => grade.Points).HasPrecision(5, 2);

        builder.HasOne<Submission>().WithMany().HasForeignKey(grade => grade.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(grade => grade.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(grade => grade.GradedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(grade => new { grade.SubmissionId, grade.StudentId })
            .IsUnique()
            .HasDatabaseName("ux_submission_member_grades_submission_id_student_id");
    }
}
