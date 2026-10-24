using System.Security.Claims;

namespace Wizer.Api.Shared.Auth;

/// <summary>
/// Reads the current user from the request claims. Only use it behind [Authorize]: a missing or
/// malformed claim here is a programming error, not a client error.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id => ParseGuid(WizerClaimTypes.Subject);

    // Exact names only: Enum.TryParse would also accept "2" or "Student,Teacher".
    public PlatformRole Role => GetClaim(WizerClaimTypes.Role) switch
    {
        "student" => PlatformRole.Student,
        "teacher" => PlatformRole.Teacher,
        "admin" => PlatformRole.Admin,
        _ => throw new InvalidOperationException("The current user has no valid role claim."),
    };

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
