namespace Wizer.Api.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Sender shown to recipients, e.g. <c>Wizer &lt;no-reply@wizer.local&gt;</c>.</summary>
    public string From { get; init; } = "Wizer <no-reply@wizer.local>";

    public string? SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 25;
}
