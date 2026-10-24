
namespace SocialNetworkPlatformProject.Extensions;

public static class MediaCookieExtensions
{
    public static void AppendMediaCookie(this HttpContext context, string token, DateTime expiresAtUtc)
    {
        context.Response.Cookies.Append(MediaCookie.Name, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = MediaCookie.Path,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true
        });
    }
}
