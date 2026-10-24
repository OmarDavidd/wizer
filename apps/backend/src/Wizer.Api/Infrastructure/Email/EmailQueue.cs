using System.Threading.Channels;

namespace Wizer.Api.Infrastructure.Email;

/// <summary>In-process queue; emails still queued when the process stops are lost (codes can be resent).</summary>
public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public void Enqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
}
