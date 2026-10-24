using System.Net;
using System.Text.Json;

namespace Wizer.IntegrationTests.Modules.Identity;

/// <summary>Reads session cookies and problem details from auth responses.</summary>
public static class AuthHttp
{
    public static IReadOnlyList<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    /// <summary>The value set for <paramref name="name"/>, or null when the response does not set it.</summary>
    public static string? CookieValue(HttpResponseMessage response, string name) =>
        SetCookies(response)
            .Where(cookie => cookie.StartsWith(name + "=", StringComparison.Ordinal))
            .Select(cookie => cookie.Split(';')[0][(name.Length + 1)..])
            .SingleOrDefault();

    public static void AssertCleared(HttpResponseMessage response, string name)
    {
        var cookie = SetCookies(response).Single(value => value.StartsWith(name + "=;", StringComparison.Ordinal) || value.StartsWith(name + "=", StringComparison.Ordinal));
        Assert.StartsWith(name + "=;", cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        return root;
    }
}
