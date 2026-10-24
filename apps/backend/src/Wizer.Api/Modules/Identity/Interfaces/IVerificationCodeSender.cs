using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Interfaces;

public interface IVerificationCodeSender
{
    /// <summary>
    /// Replaces the user's code for <paramref name="purpose"/> and emails it in the background, unless the
    /// current one is younger than 60 seconds. Callers answer the same either way.
    /// </summary>
    Task SendAsync(User user, VerificationPurpose purpose, CancellationToken cancellationToken);
}
