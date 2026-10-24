using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Campuses.DTOs;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Campuses.Interfaces;

namespace Wizer.Api.Modules.Campuses.Repositories;

public sealed class CampusRepository(AppDbContext db) : ICampusRepository
{
    public Task<CampusReference?> FindActiveAsync(Guid campusId, CancellationToken cancellationToken) =>
        db.Set<Campus>()
            .Where(campus => campus.Id == campusId && campus.IsActive)
            .Select(campus => new CampusReference(campus.Id, campus.InstitutionId))
            .SingleOrDefaultAsync(cancellationToken);
}
