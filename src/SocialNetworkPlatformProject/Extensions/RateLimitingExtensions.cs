using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SocialNetworkPlatformProject.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var authPerMinute = configuration.GetValue("RateLimiting:AuthPerMinute", 20);
        var generalPerMinute = configuration.GetValue("RateLimiting:GeneralPerMinute", 600);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientAddress(context), _ => Window(authPerMinute)));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/uploads")
                    ? RateLimitPartition.GetNoLimiter("uploads")
                    : RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(generalPerMinute)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiting")
                    .LogWarning("Too many requests: {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    message = Loc.T("rateLimit.tooMany")
                }, cancellationToken);
            };
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions Window(int permitLimit) => new()
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    };

    private static string ClientAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string ClientKey(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId != null ? $"user:{userId}" : $"ip:{ClientAddress(context)}";
    }
}
