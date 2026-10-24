namespace Wizer.Api.Shared.Observability;

/// <summary>Values that may appear in logs only masked (.ia/observability.md).</summary>
public static class LogMasking
{
    /// <summary><c>ana.perez@uni.edu</c> → <c>a***@uni.edu</c>.</summary>
    public static string Email(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
