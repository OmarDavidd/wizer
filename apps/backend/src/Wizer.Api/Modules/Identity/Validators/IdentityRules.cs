using FluentValidation;
using Wizer.Api.Modules.Identity.Classes;

namespace Wizer.Api.Modules.Identity.Validators;

/// <summary>Field rules shared by the Identity validators (.ia/authentication.md, .ia/security.md §9).</summary>
public static class IdentityRules
{
    public const int EmailMaxLength = 320;
    public const int PasswordMinLength = 10;
    public const int PasswordMaxLength = 128;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(email => EmailAddress.Normalize(email).Length <= EmailMaxLength)
            .WithMessage($"Email must be at most {EmailMaxLength} characters.")
            .EmailAddress();

    /// <summary>At least 10 characters with a letter and a digit, and different from the email.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule, Func<T, string> email) =>
        rule.NotEmpty()
            .MinimumLength(PasswordMinLength)
            .MaximumLength(PasswordMaxLength)
            .Must(password => password.Any(char.IsLetter)).WithMessage("Password must contain a letter.")
            .Must(password => password.Any(char.IsDigit)).WithMessage("Password must contain a number.")
            .Must((request, password) =>
                string.Equals(password.Trim(), EmailAddress.Normalize(email(request) ?? ""), StringComparison.OrdinalIgnoreCase) is false)
            .WithMessage("Password must not be the email.");
}
