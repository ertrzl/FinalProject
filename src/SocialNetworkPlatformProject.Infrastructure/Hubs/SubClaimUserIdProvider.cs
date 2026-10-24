using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User.FindFirst("sub")?.Value
               ?? connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
