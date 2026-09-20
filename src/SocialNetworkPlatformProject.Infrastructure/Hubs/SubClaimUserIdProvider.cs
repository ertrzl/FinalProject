using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// TokenService puts the user id in the JWT "sub" claim; SignalR's default provider only looks at NameIdentifier.
public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User.FindFirst("sub")?.Value
               ?? connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
