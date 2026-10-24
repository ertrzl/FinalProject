using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SocialNetworkPlatformProject.Filters;

public class ActionLogFilter : IAsyncActionFilter
{
    private readonly ILogger<ActionLogFilter> _logger;

    public ActionLogFilter(ILogger<ActionLogFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || executed.Exception != null)
            return;

        var status = executed.Result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            IStatusCodeActionResult statusResult => statusResult.StatusCode ?? StatusCodes.Status200OK,
            _ => context.HttpContext.Response.StatusCode
        };

        var route = context.ActionDescriptor.RouteValues;
        var targets = string.Join(", ", context.RouteData.Values
            .Where(pair => pair.Key is not ("controller" or "action"))
            .Select(pair => $"{pair.Key}={pair.Value}"));

        var created = (executed.Result as ObjectResult)?.Value;
        var createdId = created?.GetType().GetProperty("Id")?.GetValue(created);
        var details = string.Join(" ", new[] { targets.Length == 0 ? "" : $"({targets})", createdId == null ? "" : $"id={createdId}" }.Where(part => part.Length > 0));

        _logger.LogInformation("{Controller}.{Action} {Method} -> {Status} {Details}", route["controller"], route["action"], method, status, details);
    }
}
