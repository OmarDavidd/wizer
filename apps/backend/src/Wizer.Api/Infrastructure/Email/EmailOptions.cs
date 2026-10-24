namespace Wizer.Api.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>smtp</c> (Mailpit in development and CI) or <c>resend</c> (production).</summary>
    public string Provider { get; init; } = "smtp";

    /// <summary>Sender shown to recipients, e.g. <c>Wizer &lt;no-reply@wizer.local&gt;</c>.</summary>
    public string From { get; init; } = "Wizer <no-reply@wizer.local>";

    public string? SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 25;

    public string? ResendApiKey { get; init; }
}
