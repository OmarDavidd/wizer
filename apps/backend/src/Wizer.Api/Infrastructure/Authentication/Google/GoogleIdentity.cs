namespace Wizer.Api.Infrastructure.Authentication.Google;

/// <summary>
/// A Google account with a verified email; <paramref name="Subject"/> is the stable Google user id and
/// <paramref name="Name"/> the display name, when the token carries one.
/// </summary>
public sealed record GoogleIdentity(string Subject, string Email, string? Name = null);
