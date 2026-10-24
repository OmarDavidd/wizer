using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace Wizer.Api.Infrastructure.Email;

/// <summary>
/// Plain SMTP without authentication or TLS, for Mailpit in development and CI. Production uses an
/// HTTP provider (Resend), so the built-in client is enough here.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    // SmtpClient.Timeout only applies to the synchronous Send, so the async send is bounded with a token.
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is not configured.");
        }

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort);
        using var mail = new MailMessage(settings.From, message.To, message.Subject, message.TextBody)
        {
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SendTimeout);
        await client.SendMailAsync(mail, timeout.Token);
    }
}
