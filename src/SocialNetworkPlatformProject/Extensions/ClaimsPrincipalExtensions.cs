using System.Security.Claims;

namespace SocialNetworkPlatformProject.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNamesSub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }

    private const string JwtRegisteredClaimNamesSub = "sub";
}
