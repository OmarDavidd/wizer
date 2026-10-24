using Wizer.Api.Shared.Observability;

namespace Wizer.Api.Infrastructure.Email;

/// <summary>Sends queued emails one by one. A failure is logged (masked recipient, no body) and skipped.</summary>
public sealed class EmailDispatcher(EmailQueue queue, IEmailSender sender, ILogger<EmailDispatcher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await sender.SendAsync(message, stoppingToken);
            }
            // Only shutdown stops the loop: a provider timeout also surfaces as a cancellation.
            catch (Exception exception) when (stoppingToken.IsCancellationRequested is false)
            {
                logger.LogError(exception, "Email to {Recipient} with subject {Subject} could not be sent",
                    LogMasking.Email(message.To), message.Subject);
            }
        }
    }
}
