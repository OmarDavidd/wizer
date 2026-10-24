namespace Wizer.Api.Modules.Campuses.DTOs;

/// <summary>What other modules need to know about a campus.</summary>
public sealed record CampusReference(Guid Id, Guid InstitutionId);
