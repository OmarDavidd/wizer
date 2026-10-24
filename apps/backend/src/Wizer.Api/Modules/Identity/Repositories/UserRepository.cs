using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;

namespace Wizer.Api.Modules.Identity.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Set<User>().SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public void Add(User user) => db.Add(user);
}
