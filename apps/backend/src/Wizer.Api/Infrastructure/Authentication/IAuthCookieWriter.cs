namespace Wizer.Api.Infrastructure.Authentication;

public interface IAuthCookieWriter
{
    void WriteSession(AccessToken accessToken, string refreshToken, DateTimeOffset refreshExpiresAt);

    void Clear();
}
