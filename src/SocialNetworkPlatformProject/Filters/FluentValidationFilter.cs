using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace SocialNetworkPlatformProject.Filters;

// Runs the FluentValidation validator of every object an action receives (a posted form, a JSON body, a query object) and
// puts what it finds into ModelState, next to the binding errors. If anything is wrong the request is answered with the
// same 400 "errors" list [ApiController] gives for a bad model, and the action never starts.
// (This replaced the FluentValidation.AspNetCore package, which is deprecated.)
public class FluentValidationFilter : IAsyncActionFilter, IOrderedFilter
{
    // Early, before the other action filters (the log line of ActionLogFilter, for one) see a request that will be refused.
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
