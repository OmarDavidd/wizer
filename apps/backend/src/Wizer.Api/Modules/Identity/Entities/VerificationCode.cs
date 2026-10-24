using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Identity.Entities;

public enum VerificationPurpose
{
    EmailVerification,
    PasswordReset,
}

/// <summary>
/// A 6-digit code; only its SHA-256 hash is stored. Expires in 15 minutes, allows 5 attempts and is
/// single use. There is at most one unconsumed code per user and purpose (<c>.ia/authentication.md</c>).
/// </summary>
public class VerificationCode : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public VerificationPurpose Purpose { get; set; }

    public required string CodeHash { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
