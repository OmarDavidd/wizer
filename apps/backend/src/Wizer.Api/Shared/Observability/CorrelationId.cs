namespace Wizer.Api.Shared.Observability;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";

    private const string UlidAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string New() => Guid.CreateVersion7().ToString();

    /// <summary>Accepts only a UUID or a ULID, so a client cannot inject arbitrary text into the logs.</summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && (IsUuid(value) || IsUlid(value));

    private static bool IsUuid(string value) =>
        value.Length == 36 && Guid.TryParseExact(value, "D", out _);

    private static bool IsUlid(string value) =>
        value.Length == 26 && value.All(c => UlidAlphabet.Contains(char.ToUpperInvariant(c)));
}
