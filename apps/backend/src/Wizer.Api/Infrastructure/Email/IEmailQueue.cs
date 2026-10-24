namespace Wizer.Api.Infrastructure.Email;

/// <summary>
/// Hands an email to the background dispatcher and returns at once, so a request takes the same time
/// whether or not it sends anything (.ia/authentication.md: no account enumeration by timing).
/// </summary>
public interface IEmailQueue
{
    void Enqueue(EmailMessage message);
}
