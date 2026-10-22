using System.Security.Claims;

namespace Wizer.Api.Shared.Auth;

/// <summary>
/// Reads the current user from the request claims. Endpoints are [Authorize] by default, so a
/// missing or malformed claim here is a programming error, not a client error.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id => ParseGuid(WizerClaimTypes.Subject);

    public PlatformRole Role =>
        Enum.TryParse<PlatformRole>(GetClaim(WizerClaimTypes.Role), ignoreCase: true, out var role)
        && Enum.IsDefined(role)
            ? role
            : throw new InvalidOperationException("The current user has no valid role claim.");

    public Guid InstitutionId => ParseGuid(WizerClaimTypes.Institution);

    private Guid ParseGuid(string claimType) =>
        Guid.TryParse(GetClaim(claimType), out var value)
            ? value
            : throw new InvalidOperationException($"The current user has no valid '{claimType}' claim.");

    private string? GetClaim(string claimType)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated is not true)
        {
            throw new InvalidOperationException("There is no authenticated user in the current request.");
        }

        return user.FindFirstValue(claimType);
    }
}
