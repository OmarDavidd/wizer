using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Teams.Entities;

/// <summary>
/// A team inside a class, with a unique name and 2 to 10 members (BR-TEAM-01). Creating it also creates its
/// chat channel and whiteboard (BR-TEAM-05). A team with submissions is soft deleted (BR-TEAM-06).
/// </summary>
public class Team : IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
{
    public const int MinSize = 2;

    public const int MaxSizeLimit = 10;

    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public required string Name { get; set; }

    public short MaxSize { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL <c>xmin</c>. A conflicting update raises <c>DbUpdateConcurrencyException</c>.</summary>
    public uint RowVersion { get; set; }
}
