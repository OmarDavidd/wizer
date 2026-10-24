using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Wizer.Api.Infrastructure.Authentication;

/// <summary>
/// The HS256 key shared by the token issuer and the JWT handler. Without <c>Jwt:SigningKey</c>,
/// Development gets a random key per process (sessions end on restart); any other environment fails,
/// because a missing secret must never fall back to a guessable value.
/// </summary>
public sealed class JwtSigningKey
{
    private const int MinimumBytes = 32;

    public JwtSigningKey(IOptions<JwtOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.SigningKey;
        byte[] bytes;

        if (string.IsNullOrWhiteSpace(configured))
        {
            if (environment.IsDevelopment() is false)
            {
                throw new InvalidOperationException("Jwt:SigningKey is not configured.");
            }

            bytes = RandomNumberGenerator.GetBytes(MinimumBytes);
        }
        else
        {
            bytes = Encoding.UTF8.GetBytes(configured);
        }

        if (bytes.Length < MinimumBytes)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinimumBytes} bytes.");
        }

        Key = new SymmetricSecurityKey(bytes);
    }

    public SymmetricSecurityKey Key { get; }
}
