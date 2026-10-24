using Wizer.Api.Shared.Ids;

namespace Wizer.UnitTests.Shared.Ids;

public class UuidV7IdGeneratorTests
{
    [Fact]
    public void NewId_ReturnsVersion7Guid()
    {
        var id = new UuidV7IdGenerator().NewId();

        Assert.Equal(7, id.Version);
    }

    [Fact]
    public void NewId_ConsecutiveCalls_AreUnique()
    {
        var sut = new UuidV7IdGenerator();

        Assert.NotEqual(sut.NewId(), sut.NewId());
    }
}
