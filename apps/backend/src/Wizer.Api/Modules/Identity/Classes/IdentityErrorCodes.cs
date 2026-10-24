namespace Wizer.Api.Modules.Identity.Classes;

/// <summary>Identity error codes (.ia/errors.md §Identity).</summary>
public static class IdentityErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string InvalidVerificationCode = "INVALID_VERIFICATION_CODE";
    public const string VerificationPasswordMismatch = "VERIFICATION_PASSWORD_MISMATCH";
    public const string InvalidResetCode = "INVALID_RESET_CODE";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string TooManyLoginAttempts = "TOO_MANY_LOGIN_ATTEMPTS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string GoogleTokenInvalid = "GOOGLE_TOKEN_INVALID";
    public const string CampusRequired = "CAMPUS_REQUIRED";
}
