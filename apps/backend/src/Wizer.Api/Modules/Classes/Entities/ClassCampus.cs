using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Classes.Entities;

/// <summary>A campus taking part in a class (CLS-01). Key: (class_id, campus_id).</summary>
public class ClassCampus : IHasCreatedAt
{
    public Guid ClassId { get; set; }

    public Guid CampusId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
