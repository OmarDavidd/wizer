namespace Wizer.Api.Modules.Identity.DTOs.Requests;

public sealed record LoginRequest(string Email, string Password, bool RememberMe);
