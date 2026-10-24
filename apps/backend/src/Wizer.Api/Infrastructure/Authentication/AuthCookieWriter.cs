namespace Wizer.Api.Infrastructure.Authentication;

/// <summary>
/// Session cookies: <c>HttpOnly</c>, <c>Secure</c> and <c>SameSite=Strict</c> in every environment
/// (browsers accept Secure cookies on http://localhost). The refresh cookie only travels to /api/v1/auth.
/// </summary>
public sealed class AuthCookieWriter(IHttpContextAccessor httpContextAccessor) : IAuthCookieWriter
{
    public const string RefreshPath = "/api/v1/auth";

    public void WriteSession(AccessToken accessToken, string refreshToken, DateTimeOffset refreshExpiresAt)
    {
        var cookies = Response.Cookies;
        cookies.Append(AuthCookies.Access, accessToken.Value, Options("/", accessToken.ExpiresAt));
        cookies.Append(AuthCookies.Refresh, refreshToken, Options(RefreshPath, refreshExpiresAt));
    }

    public void Clear()
    {
        var cookies = Response.Cookies;
        cookies.Delete(AuthCookies.Access, Options("/", expires: null));
        cookies.Delete(AuthCookies.Refresh, Options(RefreshPath, expires: null));
    }

    private HttpResponse Response =>
        httpContextAccessor.HttpContext?.Response
        ?? throw new InvalidOperationException("Session cookies can only be written during a request.");

    private static CookieOptions Options(string path, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = path,
        Expires = expires,
        IsEssential = true,
    };
}
