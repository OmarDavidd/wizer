using System.Collections.Concurrent;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Classes;

/// <summary>
/// Login lockout (AUTH-10): 5 attempts per normalized email within a 5-minute window, then 429 until the
/// window ends. In memory: a single API instance, reset on restart (.ia/authentication.md).
/// </summary>
public sealed class LoginAttemptLimiter(IClock clock)
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private const int SweepThreshold = 10_000;
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Counter> _counters = new();
    private readonly Lock _sweepLock = new();
    private DateTimeOffset _lastSweep = DateTimeOffset.MinValue;

    /// <summary>Spends one attempt before the password is checked; throws when the window is used up.</summary>
    public void Reserve(string normalizedEmail)
    {
        var now = clock.UtcNow;
        SweepIfNeeded(now);

        while (true)
        {
            var counter = _counters.GetOrAdd(normalizedEmail, _ => new Counter(now));
            lock (counter)
            {
                // Swept between GetOrAdd and the lock: count on the counter that is in the dictionary now.
                if (counter.Removed)
                {
                    continue;
                }

                if (now >= counter.WindowStart + Window)
                {
                    counter.WindowStart = now;
                    counter.Attempts = 0;
                }

                if (counter.Attempts >= MaxAttempts)
                {
                    throw new TooManyRequestsException(
                        "Too many failed login attempts. Try again later.",
                        IdentityErrorCodes.TooManyLoginAttempts,
                        counter.WindowStart + Window - now);
                }

                counter.Attempts++;
                return;
            }
        }
    }

    /// <summary>After a correct password or a password reset.</summary>
    public void Reset(string normalizedEmail) => _counters.TryRemove(normalizedEmail, out _);

    /// <summary>Drops expired windows, at most once a minute, so many distinct emails cannot make every call scan.</summary>
    private void SweepIfNeeded(DateTimeOffset now)
    {
        if (_counters.Count <= SweepThreshold || now - _lastSweep < SweepInterval)
        {
            return;
        }

        lock (_sweepLock)
        {
            if (now - _lastSweep < SweepInterval)
            {
                return;
            }

            _lastSweep = now;
            foreach (var (email, counter) in _counters)
            {
                lock (counter)
                {
                    if (now >= counter.WindowStart + Window)
                    {
                        counter.Removed = true;
                        _counters.TryRemove(new KeyValuePair<string, Counter>(email, counter));
                    }
                }
            }
        }
    }

    private sealed class Counter(DateTimeOffset windowStart)
    {
        public DateTimeOffset WindowStart { get; set; } = windowStart;

        public int Attempts { get; set; }

        public bool Removed { get; set; }
    }
}
