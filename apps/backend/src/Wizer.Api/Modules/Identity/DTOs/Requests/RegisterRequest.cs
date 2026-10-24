namespace Wizer.Api.Modules.Identity.DTOs.Requests;

/// <summary>Public student registration. There is no role field: registration always creates a student.</summary>
public sealed record RegisterRequest(string FullName, string Email, string Password, Guid CampusId);
