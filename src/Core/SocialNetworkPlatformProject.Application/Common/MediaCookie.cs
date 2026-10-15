namespace SocialNetworkPlatformProject.Application.Common;

// Photos and videos are not public files: /uploads/... is answered by UploadsController, which asks who is looking. A
// `<img>` tag cannot send the Authorization header the API uses, so the server hands the browser a second, separate token
// in a cookie with every sign-in and token renewal. It can only be used to open uploads (it has its own audience, so the
// API refuses it, and the API's token is refused here), it is HttpOnly (a script cannot read it) and the browser sends
// it to /uploads only.
public static class MediaCookie
{
    public const string Name = "sn_media";
    public const string Path = "/uploads";

    // The name of the second JWT scheme that reads the cookie (see Infrastructure ServiceRegistration).
    public const string Scheme = "MediaCookie";
}
