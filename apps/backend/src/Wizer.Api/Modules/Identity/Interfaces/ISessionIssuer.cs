using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface ISessionIssuer
{
    /// <summary>Adds a new refresh token family for <paramref name="user"/> (saved with the unit of work) and signs an access token.</summary>
    IssuedSession Issue(User user, bool rememberMe);
}
