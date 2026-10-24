using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Wizer.Api.Modules.Identity.Classes;

/// <summary>
/// Codes and tokens are stored only as their SHA-256 (64 hex characters) and compared in constant time.
/// They are random and short-lived, so a fast hash is enough; passwords use <c>PasswordHasher</c>.
/// </summary>
public static class SecretHash
{
    public static string Of(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static bool Matches(string storedHash, string candidate) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(storedHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(candidate)));

    /// <summary>A 6-digit code, uniformly random.</summary>
    public static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>32 random bytes in Base64Url, for refresh and invitation tokens.</summary>
    public static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}
