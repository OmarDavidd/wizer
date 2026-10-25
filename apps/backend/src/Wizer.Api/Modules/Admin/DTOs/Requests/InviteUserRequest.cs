namespace Wizer.Api.Modules.Admin.DTOs.Requests;

/// <summary><paramref name="Role"/> is <c>teacher</c> or <c>admin</c>: students register themselves.</summary>
public sealed record InviteUserRequest(string FullName, string Email, string Role, Guid CampusId);
