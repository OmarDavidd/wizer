using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Classes.Entities;

/// <summary>The class role lives here, not on the user: a teacher can be a student in another class.</summary>
public enum ClassRole
{
    Teacher,
    Student,
}

/// <summary>
/// A user's membership in a class. Leaving or being removed deletes the row; messages and submissions
/// reference the user, so they are kept (BR-CLS-08). <see cref="IHasCreatedAt.CreatedAt"/> is the join date.
/// </summary>
public class ClassMembership : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid UserId { get; set; }

    public ClassRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
