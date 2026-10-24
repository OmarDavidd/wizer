using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Submissions.Entities;
using Wizer.Api.Modules.Teams.Entities;

namespace Wizer.Api.Modules.Submissions.Configurations;

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions", table =>
        {
            table.HasEnumCheck<SubmissionStatus>("status");
            table.HasEnumCheck<GradeResult>("result");
            table.HasCheckConstraint("ck_submissions_owner", "(student_id IS NULL) <> (team_id IS NULL)");
            table.HasCheckConstraint("ck_submissions_points", "points IS NULL OR points >= 0");
            table.HasCheckConstraint("ck_submissions_grade", "points IS NULL OR result IS NULL");
        });
        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.Status).HasSnakeCaseConversion();
        builder.Property(submission => submission.Result).HasSnakeCaseConversion();
        builder.Property(submission => submission.Points).HasPrecision(5, 2);
        builder.Property(submission => submission.Feedback).HasMaxLength(Submission.MaxFeedbackLength);
        builder.Property(submission => submission.RowVersion).IsRowVersion();

        builder.HasOne<Assignment>().WithMany().HasForeignKey(submission => submission.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(submission => submission.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Team>().WithMany().HasForeignKey(submission => submission.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(submission => submission.SubmittedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(submission => submission.GradedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(submission => new { submission.AssignmentId, submission.StudentId })
            .IsUnique()
            .HasFilter("student_id IS NOT NULL")
            .HasDatabaseName("ux_submissions_assignment_id_student_id");
        builder.HasIndex(submission => new { submission.AssignmentId, submission.TeamId })
            .IsUnique()
            .HasFilter("team_id IS NOT NULL")
            .HasDatabaseName("ux_submissions_assignment_id_team_id");
        builder.HasIndex(submission => new { submission.AssignmentId, submission.Status }).HasDatabaseName("ix_submissions_assignment_id_status");
        builder.HasIndex(submission => submission.StudentId).HasDatabaseName("ix_submissions_student_id");
        builder.HasIndex(submission => submission.TeamId).HasDatabaseName("ix_submissions_team_id");
    }
}
