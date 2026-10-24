using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure;

/// <summary>Starts truncated to microseconds, PostgreSQL's precision, so saved timestamps compare equal.</summary>
public sealed class TestClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start.AddTicks(-(start.Ticks % 10));

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
