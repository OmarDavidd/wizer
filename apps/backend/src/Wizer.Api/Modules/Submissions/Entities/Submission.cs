using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Submissions.Entities;

public enum SubmissionStatus
{
    Draft,
    Submitted,
    Graded,
}

public enum GradeResult
{
    Pass,
    Fail,
}

/// <summary>
/// The work of a student (individual assignment) or a team (team assignment): exactly one of
/// <see cref="StudentId"/> and <see cref="TeamId"/> is set, and there is one per owner and assignment
/// (BR-SUB-07). Never deleted. A row can also exist only to hold a grade for work never submitted (BR-GRD-03).
/// Files are <c>files</c> rows with context <c>submission</c>.
/// </summary>
public class Submission : IHasCreatedAt, IHasUpdatedAt
{
    public const int MaxFeedbackLength = 2000;

    public Guid Id { get; set; }

    public Guid AssignmentId { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? TeamId { get; set; }

    public SubmissionStatus Status { get; set; }

    /// <summary>Sent after <c>due_at</c> with late submissions allowed (BR-SUB-04).</summary>
    public bool IsLate { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>Who sent it; in a team, any member (BR-SUB-07).</summary>
    public Guid? SubmittedById { get; set; }

    /// <summary>Grade for <c>points</c> assignments, 0 to the assignment's max points (BR-GRD-02).</summary>
    public decimal? Points { get; set; }

    /// <summary>Grade for <c>pass_fail</c> assignments.</summary>
    public GradeResult? Result { get; set; }

    public string? Feedback { get; set; }

    public Guid? GradedById { get; set; }

    public DateTimeOffset? GradedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL <c>xmin</c>. A conflicting update raises <c>DbUpdateConcurrencyException</c>.</summary>
    public uint RowVersion { get; set; }
}
