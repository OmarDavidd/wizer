namespace Wizer.Api.Modules.Identity.DTOs.Responses;

/// <summary>The signed-in user. <paramref name="Role"/> is <c>student</c>, <c>teacher</c> or <c>admin</c>.</summary>
public sealed record MeResponse(Guid Id, string FullName, string Email, string Role, Guid? CampusId);
