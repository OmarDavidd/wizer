using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IExternalLoginRepository
{
    Task<Guid?> FindUserIdAsync(ExternalLoginProvider provider, string providerKey, CancellationToken cancellationToken);

    void Add(ExternalLogin login);
}
