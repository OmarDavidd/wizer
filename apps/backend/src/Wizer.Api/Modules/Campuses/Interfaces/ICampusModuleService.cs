using Wizer.Api.Modules.Campuses.DTOs;

namespace Wizer.Api.Modules.Campuses.Interfaces;

/// <summary>The Campuses module as seen by other modules.</summary>
public interface ICampusModuleService
{
    /// <summary>The campus if it exists and is active (inactive campuses are hidden from registration).</summary>
    Task<CampusReference?> FindActiveAsync(Guid campusId, CancellationToken cancellationToken);
}
