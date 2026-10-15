namespace SocialNetworkPlatformProject.Application.Common;

// How much a single request may carry when it uploads files. The default of the web server (about 30 MB) is less than
// what the limits of one post allow (up to `PostLimits.MaxImages` photos of 5 MB, or one video of 25 MB), so the actions that take files
// raise it to this; every other request keeps the default.
public static class UploadLimits
{
    public const long MaxRequestBytes = 64L * 1024 * 1024;

    public static int MaxRequestMegabytes => (int)(MaxRequestBytes / (1024 * 1024));
}
