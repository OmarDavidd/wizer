using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Repositories;

public sealed class ExternalLoginRepository(AppDbContext db) : IExternalLoginRepository
{
    public Task<Guid?> FindUserIdAsync(ExternalLoginProvider provider, string providerKey, CancellationToken cancellationToken) =>
        db.Set<ExternalLogin>()
            .Where(login => login.Provider == provider && login.ProviderKey == providerKey)
            .Select(login => (Guid?)login.UserId)
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(ExternalLogin login) => db.Add(login);
}
