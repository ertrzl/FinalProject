using System.Security.Claims;

namespace SocialNetworkPlatformProject.Extensions;

// TokenService writes the user id into the JWT "sub" claim; MapInboundClaims=false keeps it as "sub" here.
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNamesSub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }

    private const string JwtRegisteredClaimNamesSub = "sub";
}
