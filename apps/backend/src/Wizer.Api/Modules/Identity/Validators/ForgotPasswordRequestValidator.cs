using FluentValidation;
using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Validators;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(request => request.Email).ValidEmail();
}
