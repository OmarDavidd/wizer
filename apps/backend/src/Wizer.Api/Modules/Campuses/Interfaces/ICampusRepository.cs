using Wizer.Api.Modules.Campuses.DTOs;

namespace Wizer.Api.Modules.Campuses.Interfaces;

public interface ICampusRepository
{
    Task<CampusReference?> FindActiveAsync(Guid campusId, CancellationToken cancellationToken);
}
