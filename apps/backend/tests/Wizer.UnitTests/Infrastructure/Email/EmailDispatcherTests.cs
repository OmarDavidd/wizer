using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wizer.Api.Infrastructure.Email;

namespace Wizer.UnitTests.Infrastructure.Email;

public class EmailDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_SenderTimesOut_KeepsDeliveringTheNextEmails()
    {
        var queue = new EmailQueue();
        var delivered = new TaskCompletionSource<EmailMessage>();
        var sender = new Mock<IEmailSender>();
        sender.Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To == "slow@test.local"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("SMTP timeout"));
        sender.Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To == "next@test.local"), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => delivered.TrySetResult(message))
            .Returns(Task.CompletedTask);
        var dispatcher = new EmailDispatcher(queue, sender.Object, NullLogger<EmailDispatcher>.Instance);

        await dispatcher.StartAsync(CancellationToken.None);
        queue.Enqueue(new EmailMessage("slow@test.local", "Subject", "Body"));
        queue.Enqueue(new EmailMessage("next@test.local", "Subject", "Body"));

        var next = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("next@test.local", next.To);
        await dispatcher.StopAsync(CancellationToken.None);
    }
}
