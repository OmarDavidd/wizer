using FluentValidation;
using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.Password).NotEmpty().MaximumLength(IdentityRules.PasswordMaxLength);
    }
}
