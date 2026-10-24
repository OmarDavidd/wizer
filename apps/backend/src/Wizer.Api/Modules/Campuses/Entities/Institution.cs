using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Campuses.Entities;

/// <summary>The MVP seeds one institution, but users, campuses and classes already carry its id.</summary>
public class Institution : IHasCreatedAt
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
