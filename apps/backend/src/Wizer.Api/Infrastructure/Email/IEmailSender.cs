namespace Wizer.Api.Infrastructure.Email;

/// <summary>Delivers one email to the provider (SMTP in development, Resend in production).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
