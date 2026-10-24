namespace Wizer.Api.Modules.Identity.DTOs.Requests;

/// <summary>
/// The Google Identity Services credential. <paramref name="CampusId"/> is only needed the first time,
/// when the account is created; without it a new account answers 400 CAMPUS_REQUIRED.
/// </summary>
public sealed record GoogleSignInRequest(string IdToken, bool RememberMe, Guid? CampusId);
