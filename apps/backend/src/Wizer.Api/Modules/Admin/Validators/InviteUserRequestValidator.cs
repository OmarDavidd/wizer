using FluentValidation;
using Wizer.Api.Modules.Admin.DTOs.Requests;
using Wizer.Api.Modules.Identity.Validators;

namespace Wizer.Api.Modules.Admin.Validators;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.Role)
            .Must(role => role is "teacher" or "admin")
            .WithMessage("Role must be teacher or admin.");
        RuleFor(request => request.CampusId).NotEmpty();
    }
}
