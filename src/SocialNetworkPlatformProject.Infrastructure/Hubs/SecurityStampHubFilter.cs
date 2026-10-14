using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SocialNetworkPlatformProject.Infrastructure.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// The token of a connection is checked once, when the connection is made. A connection stays open for hours, so
// without this a person whose account was suspended (or whose password was changed) could keep calling the hub with
// the old token. Every call a client makes is checked again against the account's current security stamp (the same
// check as every API request, remembered for a few seconds); when it fails the connection is closed.
public class SecurityStampHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var principal = invocationContext.Context.User;
        var sub = principal?.FindFirst("sub")?.Value;
        var stamp = principal?.FindFirst(TokenService.SecurityStampClaim)?.Value;
        var validator = invocationContext.ServiceProvider.GetRequiredService<AccessTokenValidator>();

        if (!Guid.TryParse(sub, out var userId) || !await validator.IsValidAsync(userId, stamp))
        {
            invocationContext.Context.Abort();
            throw new HubException(Loc.T("auth.sessionInvalid"));
        }

        return await next(invocationContext);
    }
}
