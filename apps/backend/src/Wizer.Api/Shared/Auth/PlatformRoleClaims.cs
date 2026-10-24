namespace Wizer.Api.Shared.Auth;

public static class PlatformRoleClaims
{
    /// <summary>The <c>role</c> claim value; <see cref="HttpContextCurrentUser"/> reads these exact names back.</summary>
    public static string ToClaimValue(this PlatformRole role) => role switch
    {
        PlatformRole.Student => "student",
        PlatformRole.Teacher => "teacher",
        PlatformRole.Admin => "admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };
}
