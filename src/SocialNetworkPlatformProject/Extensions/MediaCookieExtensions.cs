
namespace SocialNetworkPlatformProject.Extensions;

public static class MediaCookieExtensions
{
    // Puts the photo cookie (see MediaCookie) on the response: HttpOnly, sent to /uploads only, and Secure whenever the site
    // is opened over HTTPS (the development address is plain HTTP, where a Secure cookie would never come back).
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
