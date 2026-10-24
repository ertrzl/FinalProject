using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using SocialNetworkPlatformProject.Application.Exceptions.Base;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Middlewares;

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
            using var who = Who(context);
            _logger.LogInformation("{Method} {Path} refused ({Status}): {Message}", context.Request.Method, context.Request.Path, ex.StatusCode, ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            using var who = Who(context);
            _logger.LogInformation("{Method} {Path} refused (413): the upload is larger than {Limit} MB", context.Request.Method, context.Request.Path, UploadLimits.MaxRequestMegabytes);
            await WriteErrorAsync(context, StatusCodes.Status413PayloadTooLarge, Loc.T("upload.tooLarge", UploadLimits.MaxRequestMegabytes));
        }
        catch (DbUpdateConcurrencyException)
        {
            using var who = Who(context);
            _logger.LogWarning("{Method} {Path}: the data was changed by someone else at the same time", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, Loc.T("server.changedMeanwhile"));
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
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
