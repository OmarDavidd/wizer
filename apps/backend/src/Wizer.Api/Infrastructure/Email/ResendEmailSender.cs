using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Wizer.Api.Infrastructure.Email;

/// <summary>
/// Production email through the Resend HTTP API (no SDK). Resend only delivers to any recipient once the
/// sending domain is verified (.ia/infrastructure.md).
/// </summary>
public sealed class ResendEmailSender(IHttpClientFactory httpClients, IOptions<EmailOptions> options) : IEmailSender
{
    public const string HttpClientName = "resend";

    public static readonly Uri Endpoint = new("https://api.resend.com/emails");

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ResendApiKey))
        {
            throw new InvalidOperationException("Email:ResendApiKey is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                from = settings.From,
                to = new[] { message.To },
                subject = message.Subject,
                text = message.TextBody,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ResendApiKey);

        // A client per send: the factory rotates its handlers, so a singleton sender never pins a stale connection.
        using var response = await httpClients.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
