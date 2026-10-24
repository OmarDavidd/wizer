using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Modules.Identity.Classes;

public sealed class VerificationCodeSender(
    IVerificationCodeRepository codes,
    IEmailQueue emails,
    IClock clock) : IVerificationCodeSender
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    public const int MaxAttempts = 5;

    public async Task SendAsync(User user, VerificationPurpose purpose, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var pendingSince = await codes.FindPendingCreatedAtAsync(user.Id, purpose, cancellationToken);
        if (pendingSince is not null && now - pendingSince < ResendCooldown)
        {
            return;
        }

        var code = SecretHash.NewCode();
        try
        {
            await codes.ReplaceAsync(new VerificationCode
            {
                UserId = user.Id,
                Purpose = purpose,
                CodeHash = SecretHash.Of(code),
                ExpiresAt = now + Lifetime,
            }, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // A concurrent request issued a code at the same moment: that one is sent instead.
            return;
        }

        emails.Enqueue(IdentityEmails.VerificationCode(user, purpose, code));
    }
}
