using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
}
