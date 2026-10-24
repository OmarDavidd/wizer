namespace Wizer.Api.Infrastructure.Authentication;

/// <summary>Access and refresh token settings (.ia/authentication.md). Env vars: <c>Jwt__SigningKey</c>, etc.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 secret, at least 32 bytes. Generate it with <c>openssl rand -base64 48</c>.</summary>
    public string? SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    public int RefreshTokenRememberDays { get; init; } = 30;
}
