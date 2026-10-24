using FluentValidation;
using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Validators;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Code must be 6 digits.");
        RuleFor(request => request.Password).NotEmpty().MaximumLength(IdentityRules.PasswordMaxLength);
    }
}
