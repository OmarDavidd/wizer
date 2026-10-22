namespace Wizer.Api.Shared.Auth;

/// <summary>
/// Claim names inside the access token (.ia/authentication.md). The JWT handler must keep the
/// original names (<c>MapInboundClaims = false</c>) so they arrive unchanged.
/// </summary>
public static class WizerClaimTypes
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string Institution = "inst";
}
