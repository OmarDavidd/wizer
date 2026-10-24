using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Submissions.Entities;

/// <summary>
/// A per-student adjustment of a team submission's grade. When present it overrides the team grade for that
/// student (BR-GRD-04).
/// </summary>
public class SubmissionMemberGrade : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid StudentId { get; set; }

    public decimal? Points { get; set; }

    public GradeResult? Result { get; set; }

    public Guid GradedById { get; set; }

    public DateTimeOffset GradedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
