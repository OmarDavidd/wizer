namespace Wizer.Api.Shared.Time;

/// <summary>Server time. Never use DateTime.Now or DateTime.UtcNow directly; tests replace this.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
