using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IUserRepository
{
    /// <summary>Tracked, so changes are saved with the unit of work. <paramref name="normalizedEmail"/> must be normalized.</summary>
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Tracked.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}
