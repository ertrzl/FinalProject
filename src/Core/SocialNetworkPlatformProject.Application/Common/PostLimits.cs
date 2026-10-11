namespace SocialNetworkPlatformProject.Application.Common;

public static class PostLimits
{
    // A post carries up to this many photos, or exactly one video (never both).
    public const int MaxImages = 10;

    // "@name" tags in one text that are looked at (more are ignored, so nobody can tag the whole site in one post).
    public const int MaxMentions = 10;
}
