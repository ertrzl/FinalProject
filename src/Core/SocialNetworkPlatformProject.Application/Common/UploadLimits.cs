namespace SocialNetworkPlatformProject.Application.Common;

public static class UploadLimits
{
    public const long MaxRequestBytes = 64L * 1024 * 1024;

    public static int MaxRequestMegabytes => (int)(MaxRequestBytes / (1024 * 1024));
}
