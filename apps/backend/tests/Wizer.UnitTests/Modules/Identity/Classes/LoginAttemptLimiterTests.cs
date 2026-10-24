using Moq;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Shared.Exceptions;
using Wizer.Api.Shared.Time;

namespace Wizer.UnitTests.Modules.Identity.Classes;

public class LoginAttemptLimiterTests
{
    private DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reserve_FiveAttemptsInWindow_AreAllowed()
    {
        var limiter = CreateLimiter();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            limiter.Reserve("ana@uni.edu");
        }
    }

    [Fact]
    public void Reserve_SixthAttemptInWindow_Throws429WithRemainingWindow()
    {
        var limiter = CreateLimiter();
        ReserveTimes(limiter, "ana@uni.edu", 5);
        _now = _now.AddMinutes(2);

        var exception = Assert.Throws<TooManyRequestsException>(() => limiter.Reserve("ana@uni.edu"));

        Assert.Equal("TOO_MANY_LOGIN_ATTEMPTS", exception.Code);
        Assert.Equal(TimeSpan.FromMinutes(3), exception.RetryAfter);
    }

    [Fact]
    public void Reserve_AfterTheWindowEnds_AllowsAgain()
    {
        var limiter = CreateLimiter();
        ReserveTimes(limiter, "ana@uni.edu", 5);
        _now = _now.AddMinutes(5);

        limiter.Reserve("ana@uni.edu");
    }

    [Fact]
    public void Reserve_CountsPerEmail()
    {
        var limiter = CreateLimiter();
        ReserveTimes(limiter, "ana@uni.edu", 5);

        limiter.Reserve("luis@uni.edu");
    }

    [Fact]
    public void Reset_ClearsTheFailures()
    {
        var limiter = CreateLimiter();
        ReserveTimes(limiter, "ana@uni.edu", 5);

        limiter.Reset("ana@uni.edu");

        ReserveTimes(limiter, "ana@uni.edu", 5);
    }

    [Fact]
    public async Task Reserve_ConcurrentAttempts_NeverAllowMoreThanFive()
    {
        var limiter = CreateLimiter();
        var allowed = 0;

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                limiter.Reserve("ana@uni.edu");
                Interlocked.Increment(ref allowed);
            }
            catch (TooManyRequestsException)
            {
            }
        })));

        Assert.Equal(5, allowed);
    }

    private LoginAttemptLimiter CreateLimiter()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(() => _now);
        return new LoginAttemptLimiter(clock.Object);
    }

    private static void ReserveTimes(LoginAttemptLimiter limiter, string email, int times)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            limiter.Reserve(email);
        }
    }
}
