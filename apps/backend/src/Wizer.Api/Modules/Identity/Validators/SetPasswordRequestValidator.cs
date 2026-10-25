using FluentValidation;
using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Validators;

public sealed class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(request => request.Token).NotEmpty().MaximumLength(200);
        // The email is not in the request: the "not the email" rule is checked by the service.
        RuleFor(request => request.Password).StrongPassword(_ => "");
    }
}
