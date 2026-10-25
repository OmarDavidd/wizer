namespace Wizer.Api.Modules.Identity.DTOs.Requests;

/// <summary>The invited person's first password, with the token from the activation link.</summary>
public sealed record SetPasswordRequest(string Token, string Password);
