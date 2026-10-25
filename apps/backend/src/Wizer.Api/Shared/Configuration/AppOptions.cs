namespace Wizer.Api.Shared.Configuration;

/// <summary>Public facts about the deployment. Env var: <c>App__PublicUrl</c> (<c>APP_PUBLIC_URL</c> in Compose).</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Where users open the app; links in emails point here. No trailing slash.</summary>
    public string PublicUrl { get; init; } = "http://localhost:5173";
}
