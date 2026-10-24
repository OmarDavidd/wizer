using System.Collections.Concurrent;
using Wizer.Api.Infrastructure.Email;

namespace Wizer.IntegrationTests.Infrastructure;

/// <summary>Captures the emails the background dispatcher sends.</summary>
public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _sent.Where(message => message.To == address).ToList();

    /// <summary>Waits for the background dispatcher to deliver <paramref name="count"/> emails to <paramref name="address"/>.</summary>
    public async Task<IReadOnlyList<EmailMessage>> WaitForAsync(string address, int count = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var messages = SentTo(address);
            if (messages.Count >= count)
            {
                return messages;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException($"Expected {count} email(s) to {address}, got {SentTo(address).Count}.");
    }

    /// <summary>Gives the dispatcher time to deliver anything queued, to assert that nothing was sent.</summary>
    public static Task SettleAsync() => Task.Delay(300);
}
