namespace Wizer.Api.Modules.Identity.Classes;

public static class EmailAddress
{
    /// <summary>The stored form of an email: trimmed and lowercase (<c>users.email</c> is unique on it).</summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
