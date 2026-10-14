using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using SocialNetworkPlatformProject.Application.Exceptions.Base;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Middlewares;

// Turns any BaseException thrown by a service into its HTTP status + a small JSON body;
// anything unexpected becomes a generic 500 so internals never leak to the client.
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BaseException ex)
        {
            // The reason a request was refused (the request line only has the status code).
            using var who = Who(context);
            _logger.LogInformation("{Method} {Path} refused ({Status}): {Message}", context.Request.Method, context.Request.Path, ex.StatusCode, ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed (or deleted) the same row between our read and our save.
            using var who = Who(context);
            _logger.LogWarning("{Method} {Path}: the data was changed by someone else at the same time", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status409Conflict,
                "This was just changed by someone else. Please refresh and try again.");
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two requests slipped past the "does it exist yet?" check at the same moment; the unique index
            // caught the second one. To the client that is the same as being told "already exists".
            using var who = Who(context);
            _logger.LogWarning("{Method} {Path}: two requests created the same thing at once (unique index)", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, Loc.T("server.exists"));
        }
        catch (Exception ex)
        {
            using var who = Who(context);
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, Loc.T("server.unexpected"));
        }
    }

    // This middleware runs before the one that adds the user to the log lines (LogContextMiddleware needs authentication
    // first), so the lines written here get the user and the address themselves.
    private static IDisposable Who(HttpContext context)
    {
        var user = LogContext.PushProperty("UserId", RequestLogProperties.UserId(context));
        var address = LogContext.PushProperty("ClientIp", RequestLogProperties.ClientIp(context));
        return new Both(user, address);
    }

    private sealed class Both : IDisposable
    {
        private readonly IDisposable _first;
        private readonly IDisposable _second;

        public Both(IDisposable first, IDisposable second)
        {
            _first = first;
            _second = second;
        }

        public void Dispose()
        {
            _second.Dispose();
            _first.Dispose();
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { statusCode, message });
    }
}
