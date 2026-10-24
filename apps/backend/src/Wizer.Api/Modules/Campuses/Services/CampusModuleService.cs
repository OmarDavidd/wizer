using Wizer.Api.Modules.Campuses.DTOs;
using Wizer.Api.Modules.Campuses.Interfaces;

namespace Wizer.Api.Modules.Campuses.Services;

public sealed class CampusModuleService(ICampusRepository campuses) : ICampusModuleService
{
    public Task<CampusReference?> FindActiveAsync(Guid campusId, CancellationToken cancellationToken) =>
        campuses.FindActiveAsync(campusId, cancellationToken);
}
