using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Whiteboard.Entities;

/// <summary>
/// A team's whiteboard (BR-WB-01). <see cref="Scene"/> is the Excalidraw elements array as JSON, tombstones
/// included; <see cref="Version"/> grows with every snapshot the worker saves (<c>.ia/realtime.md</c>).
/// </summary>
public class TeamWhiteboard : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string Scene { get; set; } = "[]";

    public long Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL <c>xmin</c>. A conflicting update raises <c>DbUpdateConcurrencyException</c>.</summary>
    public uint RowVersion { get; set; }
}
