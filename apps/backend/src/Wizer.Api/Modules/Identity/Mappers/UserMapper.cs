using Wizer.Api.Modules.Identity.DTOs.Responses;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Modules.Identity.Mappers;

public static class UserMapper
{
    public static MeResponse ToMe(this User user) =>
        new(user.Id, user.FullName, user.Email, user.PlatformRole.ToClaimValue(), user.CampusId);
}
