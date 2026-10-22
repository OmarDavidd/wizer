namespace Wizer.Api.Infrastructure.Antivirus.ClamAV;

public sealed class ClamAvOptions
{
    public const string SectionName = "ClamAv";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3310;
}
