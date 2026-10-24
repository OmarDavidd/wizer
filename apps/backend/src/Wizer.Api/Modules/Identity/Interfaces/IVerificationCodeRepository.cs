using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IVerificationCodeRepository
{
    /// <summary>When the current unconsumed code was created, if there is one.</summary>
    Task<DateTimeOffset?> FindPendingCreatedAtAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken);

    /// <summary>Deletes the user's unconsumed code for the purpose and saves <paramref name="code"/> in its place.</summary>
    Task ReplaceAsync(VerificationCode code, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically spends one attempt on the pending code when it is unexpired and has attempts left, and
    /// returns it; <c>null</c> when there is nothing to try. Runs before comparing, so concurrent guesses
    /// can never exceed <paramref name="maxAttempts"/>.
    /// </summary>
    Task<VerificationCode?> ReserveAttemptAsync(
        Guid userId,
        VerificationPurpose purpose,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken cancellationToken);

    /// <summary>Marks the code consumed; <c>false</c> if another request consumed it first.</summary>
    Task<bool> ConsumeAsync(Guid codeId, DateTimeOffset now, CancellationToken cancellationToken);
}
