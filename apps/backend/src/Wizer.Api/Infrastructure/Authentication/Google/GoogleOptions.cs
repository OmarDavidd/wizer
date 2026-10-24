namespace Wizer.Api.Infrastructure.Authentication.Google;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>The OAuth client id; ID tokens must have it as audience. Env var: <c>Google__ClientId</c>.</summary>
    public string? ClientId { get; init; }
}
