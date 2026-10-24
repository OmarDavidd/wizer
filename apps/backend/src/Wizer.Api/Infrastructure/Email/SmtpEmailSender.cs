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
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is not configured.");
        }

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort) { Timeout = 10_000 };
        using var mail = new MailMessage(settings.From, message.To, message.Subject, message.TextBody)
        {
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
