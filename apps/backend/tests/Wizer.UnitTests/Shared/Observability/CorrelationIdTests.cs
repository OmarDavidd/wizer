using Wizer.Api.Shared.Observability;

namespace Wizer.UnitTests.Shared.Observability;

public class CorrelationIdTests
{
    [Theory]
    [InlineData("0192f3a4-5b6c-7d8e-9f01-23456789abcd")]
    [InlineData("01JAB3CDEFGHJKMNPQRSTVWXYZ")]
    public void IsValid_UuidOrUlid_ReturnsTrue(string value)
    {
        Assert.True(CorrelationId.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("01JAB3CDEFGHJKMNPQRSTVWXYI")]
    [InlineData("0192f3a4-5b6c-7d8e-9f01-23456789abcd\nforged log line")]
    public void IsValid_AnythingElse_ReturnsFalse(string? value)
    {
        Assert.False(CorrelationId.IsValid(value));
    }

    [Fact]
    public void New_ReturnsValidId()
    {
        Assert.True(CorrelationId.IsValid(CorrelationId.New()));
    }
}
