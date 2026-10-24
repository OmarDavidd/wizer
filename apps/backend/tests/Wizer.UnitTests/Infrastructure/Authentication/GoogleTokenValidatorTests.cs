using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Wizer.Api.Infrastructure.Authentication.Google;

namespace Wizer.UnitTests.Infrastructure.Authentication;

public class GoogleTokenValidatorTests
{
    private const string ClientId = "wizer-test.apps.googleusercontent.com";

    private readonly RsaSecurityKey _googleKey = new(RSA.Create(2048)) { KeyId = "google-key" };

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsSubjectAndEmail()
    {
        var identity = await CreateValidator().ValidateAsync(Token(), CancellationToken.None);

        Assert.NotNull(identity);
        Assert.Equal("google-sub-1", identity.Subject);
        Assert.Equal("ana@gmail.com", identity.Email);
        Assert.Equal("Ana Pérez", identity.Name);
    }

    [Theory]
    [InlineData("https://accounts.google.com")]
    [InlineData("accounts.google.com")]
    public async Task ValidateAsync_BothGoogleIssuers_AreAccepted(string issuer)
    {
        Assert.NotNull(await CreateValidator().ValidateAsync(Token(issuer: issuer), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_OtherAudience_ReturnsNull()
    {
        Assert.Null(await CreateValidator().ValidateAsync(Token(audience: "another-app"), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_OtherIssuer_ReturnsNull()
    {
        Assert.Null(await CreateValidator().ValidateAsync(Token(issuer: "https://evil.example"), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_Expired_ReturnsNull()
    {
        Assert.Null(await CreateValidator().ValidateAsync(Token(expires: DateTime.UtcNow.AddMinutes(-10)), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_SignedByAnotherKey_ReturnsNull()
    {
        var token = Token(signingKey: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "google-key" });

        Assert.Null(await CreateValidator().ValidateAsync(token, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task ValidateAsync_EmailNotVerified_ReturnsNull(bool? emailVerified)
    {
        Assert.Null(await CreateValidator().ValidateAsync(Token(emailVerified: emailVerified), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_Garbage_ReturnsNull()
    {
        Assert.Null(await CreateValidator().ValidateAsync("not-a-jwt", CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_ClientIdNotConfigured_ReturnsNull()
    {
        Assert.Null(await CreateValidator(clientId: null).ValidateAsync(Token(), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_TokenSignedWithARotatedKey_RefreshesGoogleKeysOnceAndAccepts()
    {
        var rotatedKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "rotated-key" };
        var configuration = new RotatingConfigurationManager(_googleKey, rotatedKey);
        var validator = new GoogleTokenValidator(
            Options.Create(new GoogleOptions { ClientId = ClientId }),
            configuration,
            NullLogger<GoogleTokenValidator>.Instance);

        var identity = await validator.ValidateAsync(Token(signingKey: rotatedKey), CancellationToken.None);

        Assert.NotNull(identity);
        Assert.Equal(1, configuration.Refreshes);
    }

    private GoogleTokenValidator CreateValidator(string? clientId = ClientId)
    {
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(_googleKey);
        return new GoogleTokenValidator(
            Options.Create(new GoogleOptions { ClientId = clientId }),
            new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            NullLogger<GoogleTokenValidator>.Instance);
    }

    private string Token(
        string issuer = "https://accounts.google.com",
        string audience = ClientId,
        DateTime? expires = null,
        bool? emailVerified = true,
        SecurityKey? signingKey = null)
    {
        var claims = new List<Claim>
        {
            new("sub", "google-sub-1"),
            new("email", "ana@gmail.com"),
            new("name", "Ana Pérez"),
        };
        if (emailVerified is not null)
        {
            claims.Add(new Claim("email_verified", emailVerified.Value ? "true" : "false", ClaimValueTypes.Boolean));
        }

        var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(30);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            NotBefore = expiresAt.AddHours(-1),
            IssuedAt = expiresAt.AddHours(-1),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey ?? _googleKey, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <summary>Serves the old key until a refresh is requested, then the rotated one.</summary>
    private sealed class RotatingConfigurationManager(SecurityKey oldKey, SecurityKey newKey)
        : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public int Refreshes { get; private set; }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            var configuration = new OpenIdConnectConfiguration();
            configuration.SigningKeys.Add(Refreshes == 0 ? oldKey : newKey);
            return Task.FromResult(configuration);
        }

        public void RequestRefresh() => Refreshes++;
    }
}
