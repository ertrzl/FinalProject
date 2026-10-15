using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Infrastructure.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// The token of a connection is checked once, when the connection is made. A connection stays open for hours, so
// without this a person whose account was suspended (or whose password was changed) could keep calling the hub with
// the old token. Every call a client makes is checked again against the account's current security stamp (the same
// check as every API request, remembered for a few seconds); when it fails the connection is closed.
//
// Two more things happen here because every hub call and every connection passes through:
//  - the connections are written down (HubConnectionRegistry), so the ones of an account that was suspended or deleted can be
//    closed at once, even a listen-only one that never calls anything;
//  - the calls are counted, because the HTTP rate limiter never sees them (one WebSocket could send messages and typing
//    events without end, each one costing database work).
public class SecurityStampHubFilter : IHubFilter
{
    // Per person, over all their tabs. Far above what typing and chatting need, far below what a script would do.
    private const int MaxCallsPerWindow = 50;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    private sealed class Counter
    {
        public int Calls;
    }

    private readonly HubConnectionRegistry _registry;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecurityStampHubFilter> _logger;

    public SecurityStampHubFilter(HubConnectionRegistry registry, IMemoryCache cache, ILogger<SecurityStampHubFilter> logger)
    {
        _registry = registry;
        _cache = cache;
        _logger = logger;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var principal = context.Context.User;
        if (Guid.TryParse(principal?.FindFirst("sub")?.Value, out var userId))
            _registry.Add(userId, principal?.FindFirst(TokenService.SecurityStampClaim)?.Value, principal?.FindFirst(TokenService.SessionClaim)?.Value, context.Context);

        await next(context);
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        if (Guid.TryParse(context.Context.User?.FindFirst("sub")?.Value, out var userId))
            _registry.Remove(userId, context.Context.ConnectionId);

        await next(context, exception);
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var principal = invocationContext.Context.User;
        var sub = principal?.FindFirst("sub")?.Value;
        var stamp = principal?.FindFirst(TokenService.SecurityStampClaim)?.Value;
        var tokenId = principal?.FindFirst("jti")?.Value;
        var sessionId = principal?.FindFirst(TokenService.SessionClaim)?.Value;
        var validator = invocationContext.ServiceProvider.GetRequiredService<AccessTokenValidator>();

        if (!Guid.TryParse(sub, out var userId) || !await validator.IsValidAsync(userId, stamp, tokenId, sessionId))
        {
            invocationContext.Context.Abort();
            throw new HubException(Loc.T("auth.sessionInvalid"));
        }

        var counter = _cache.GetOrCreate($"hub-calls:{userId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return new Counter();
        })!;

        if (Interlocked.Increment(ref counter.Calls) > MaxCallsPerWindow)
        {
            if (counter.Calls == MaxCallsPerWindow + 1)
                _logger.LogWarning("Hub call {Method:l} refused: user {UserId:l} sent more than {Limit} calls in {Seconds} seconds",
                    invocationContext.HubMethodName, userId.ToString(), MaxCallsPerWindow, Window.TotalSeconds);

            throw new HubException(Loc.T("hub.tooManyCalls"));
        }

        return await next(invocationContext);
    }
}
