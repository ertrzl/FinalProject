using Serilog.Context;

namespace SocialNetworkPlatformProject.Middlewares;

// Puts who is asking on every log line written while the request is handled (the services' own lines too): the signed-in
// person's id ("-" when nobody is signed in) and the client's address. It runs after authentication, so the user is known.
public class LogContextMiddleware
{
    private readonly RequestDelegate _next;

    public LogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("UserId", RequestLogProperties.UserId(context)))
        using (LogContext.PushProperty("ClientIp", RequestLogProperties.ClientIp(context)))
        {
            await _next(context);
        }
    }
}

public static class RequestLogProperties
{
    public static string UserId(HttpContext context) => context.User.FindFirst("sub")?.Value ?? "-";

    public static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "-";
}
