using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Notifications.Entities;

/// <summary>The events of <c>.ia/business-rules.md</c> §11.</summary>
public enum NotificationType
{
    AssignmentPublished,
    AssignmentDueDateChanged,
    AssignmentDueSoon,
    SubmissionGraded,
    TeamAssigned,
    ChatMention,
    ChatAccessRequested,
    ChatAccessResolved,
    FileRejected,
}

/// <summary>
/// An in-app notification. <see cref="Data"/> is a JSON object with the ids the UI needs to link the
/// notification (class, assignment, team…). Read notifications are deleted after 90 days (BR-NOT-02).
/// </summary>
public class Notification : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public required string Title { get; set; }

    public string? Body { get; set; }

    public string? Data { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
