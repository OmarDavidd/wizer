namespace Wizer.Api.Modules.Identity.DTOs.Requests;

public sealed record VerifyEmailRequest(string Email, string Code, string Password);
