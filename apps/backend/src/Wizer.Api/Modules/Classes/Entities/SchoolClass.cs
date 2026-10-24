using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Classes.Entities;

/// <summary>
/// A class. Archiving is manual and leaves it read-only (BR-CLS-07); deleting is a soft delete (BR-CLS-09).
/// Its single teacher is the <see cref="ClassMembership"/> with role <see cref="ClassRole.Teacher"/>.
/// </summary>
public class SchoolClass : IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
{
    /// <summary>Alphabet of the join code: no 0/O or 1/I/L (BR-CLS-02).</summary>
    public const string JoinCodeAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public const int JoinCodeLength = 6;

    public Guid Id { get; set; }

    public Guid InstitutionId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Free text, for example "Martes y jueves 18:00–20:00".</summary>
    public string? Schedule { get; set; }

    /// <summary>Unique among classes that are neither archived nor deleted.</summary>
    public required string JoinCode { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
