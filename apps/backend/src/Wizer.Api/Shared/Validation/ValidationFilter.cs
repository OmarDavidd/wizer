using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Wizer.Api.Shared.Validation;

/// <summary>
/// Runs the FluentValidation validator of every action argument before the action. Failures throw
/// <see cref="ValidationException"/>, which the global handler turns into 400 VALIDATION_ERROR.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is IValidator validator)
            {
                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    context.HttpContext.RequestAborted);
                if (result.IsValid is false)
                {
                    throw new ValidationException(result.Errors);
                }
            }
        }

        await next();
    }
}
