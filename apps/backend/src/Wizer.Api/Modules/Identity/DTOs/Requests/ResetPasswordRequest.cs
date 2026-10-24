namespace Wizer.Api.Modules.Identity.DTOs.Requests;

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
