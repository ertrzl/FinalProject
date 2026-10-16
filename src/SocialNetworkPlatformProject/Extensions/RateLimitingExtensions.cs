using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SocialNetworkPlatformProject.Extensions;

// Request limits, counted per minute:
//  - "auth" policy (login, register, refresh): per client address, small. The account lockout stops guessing one
//    account; this stops one machine from trying a password against thousands of accounts or mass-registering.
//  - a general limit on everything else, per signed-in user (per address when anonymous), generous enough that
//    normal use never notices it. Both numbers can be changed in appsettings ("RateLimiting").
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

            // The pictures of a page (/uploads) are not counted: a feed loads dozens at once, and they used to be plain static files.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/uploads")
                    ? RateLimitPartition.GetNoLimiter("uploads")
                    : RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(generalPerMinute)));

            // Same body shape as every other error the API returns.
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
