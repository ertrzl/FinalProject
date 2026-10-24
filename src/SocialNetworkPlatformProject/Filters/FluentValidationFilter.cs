using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace SocialNetworkPlatformProject.Filters;

public class FluentValidationFilter : IAsyncActionFilter, IOrderedFilter
{
    public int Order => -2500;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null || argument is string || argument.GetType().IsValueType)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            var behavior = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            context.Result = behavior.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }
}
