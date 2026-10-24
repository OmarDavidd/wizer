using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Assignments.Entities;

public enum AssignmentMode
{
    Individual,
    Team,
}

public enum GradingType
{
    Points,
    PassFail,
}

public enum AssignmentStatus
{
    /// <summary>Not visible to students.</summary>
    Draft,

    /// <summary>Visible; publishing notifies the students (BR-ASG-03).</summary>
    Published,
}

/// <summary>
/// An assignment of a class (BR-ASG-01, 02). <see cref="MaxPoints"/> is 1–100 for <see cref="GradingType.Points"/>
/// and null for <see cref="GradingType.PassFail"/>. <see cref="LateUntil"/> only applies when late submissions
/// are allowed; without it the deadline stays open (BR-SUB-03).
/// </summary>
public class Assignment : IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public required string Title { get; set; }

    public required string Instructions { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public AssignmentMode Mode { get; set; }

    public GradingType GradingType { get; set; }

    public decimal? MaxPoints { get; set; }

    public bool AllowLateSubmissions { get; set; }

    public DateTimeOffset? LateUntil { get; set; }

    public AssignmentStatus Status { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Set once the 24 h reminder was sent, so a restart does not send it twice (NOT-02).</summary>
    public DateTimeOffset? ReminderSentAt { get; set; }

    public Guid CreatedById { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL <c>xmin</c>. A conflicting update raises <c>DbUpdateConcurrencyException</c>.</summary>
    public uint RowVersion { get; set; }
}
