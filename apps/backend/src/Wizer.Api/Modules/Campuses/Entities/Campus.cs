using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Campuses.Entities;

public class Campus : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; set; }

    public Guid InstitutionId { get; set; }

    public required string Name { get; set; }

    /// <summary>An inactive campus is hidden from registration but keeps its users.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
