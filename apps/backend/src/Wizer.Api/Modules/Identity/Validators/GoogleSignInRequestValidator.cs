using FluentValidation;
using Wizer.Api.Modules.Identity.DTOs.Requests;

namespace Wizer.Api.Modules.Identity.Validators;

public sealed class GoogleSignInRequestValidator : AbstractValidator<GoogleSignInRequest>
{
    public GoogleSignInRequestValidator()
    {
        RuleFor(request => request.IdToken).NotEmpty().MaximumLength(4096);
        RuleFor(request => request.CampusId).NotEqual(Guid.Empty);
    }
}
