namespace Wizer.Api.Shared.Auth;

/// <summary>Platform role policies for <c>[Authorize(Policy = ...)]</c> (.ia/permissions.md).</summary>
public static class AuthPolicies
{
    public const string Teacher = "Teacher";
    public const string Admin = "Admin";
}
