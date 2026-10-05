using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Exceptions.Base;

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
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed (or deleted) the same row between our read and our save.
            await WriteErrorAsync(context, StatusCodes.Status409Conflict,
                "This was just changed by someone else. Please refresh and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two requests slipped past the "does it exist yet?" check at the same moment; the unique index
            // caught the second one. To the client that is the same as being told "already exists".
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "This already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    // SQL Server error 2601 (unique index) / 2627 (unique constraint).
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException { Number: 2601 or 2627 };
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
