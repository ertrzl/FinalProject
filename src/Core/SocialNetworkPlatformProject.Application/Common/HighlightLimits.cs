namespace SocialNetworkPlatformProject.Application.Common;

// Story highlights on a profile (StoryHighlightService).
public static class HighlightLimits
{
    // The longest name of a highlight; the round covers on the profile show it under the picture.
    public const int MaxTitleLength = 30;

    // How many highlights one person may have, and how many stories one highlight holds (every item is a copy of a file on the disk).
    public const int MaxPerUser = 20;
    public const int MaxItems = 30;
}
