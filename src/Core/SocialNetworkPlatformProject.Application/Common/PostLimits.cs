namespace SocialNetworkPlatformProject.Application.Common;

public static class PostLimits
{
    // A post carries up to this many photos, or exactly one video (never both).
    public const int MaxImages = 10;

    // "@name" tags in one text that are looked at (more are ignored, so nobody can tag the whole site in one post).
    public const int MaxMentions = 10;

    // Hashtags of one text that become searchable tags: the longest tag (the Hashtags table keeps 100 characters) and how
    // many a post may have. A longer tag, or the ones after the limit, stay plain text; nothing fails.
    public const int MaxHashtagLength = 100;
    public const int MaxHashtagsPerPost = 30;
}
