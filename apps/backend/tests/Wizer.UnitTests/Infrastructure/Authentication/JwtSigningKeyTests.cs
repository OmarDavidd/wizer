using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Wizer.Api.Infrastructure.Authentication;

namespace Wizer.UnitTests.Infrastructure.Authentication;

public class JwtSigningKeyTests
{
    [Fact]
    public void Constructor_ConfiguredKey_UsesItsUtf8Bytes()
    {
        var key = new JwtSigningKey(Options("a-signing-key-that-has-at-least-32-bytes"), Environment(Environments.Production));

        Assert.Equal(40, key.Key.KeySize / 8);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingKeyOutsideDevelopment_Throws(string? configured)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new JwtSigningKey(Options(configured), Environment(Environments.Production)));

        Assert.Contains("Jwt:SigningKey", exception.Message);
    }

    [Fact]
    public void Constructor_MissingKeyInDevelopment_GeneratesEphemeral256BitKey()
    {
        var first = new JwtSigningKey(Options(null), Environment(Environments.Development));
        var second = new JwtSigningKey(Options(null), Environment(Environments.Development));

        Assert.Equal(256, first.Key.KeySize);
        Assert.NotEqual(first.Key.Key, second.Key.Key);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Constructor_KeyShorterThan32Bytes_Throws(string environment)
    {
        Assert.Throws<InvalidOperationException>(
            () => new JwtSigningKey(Options("too-short-key"), Environment(environment)));
    }

    private static IOptions<JwtOptions> Options(string? signingKey) =>
        Microsoft.Extensions.Options.Options.Create(new JwtOptions { SigningKey = signingKey });

    private static IHostEnvironment Environment(string name) =>
        Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == name);
}
