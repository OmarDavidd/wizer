using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure;

public sealed class TestClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
