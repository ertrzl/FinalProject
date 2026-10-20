namespace SocialNetworkPlatformProject.Application.Common;

// The server's rules for what a group holds (the pages only show them). The rule lengths are also the database columns'.
public static class GroupLimits
{
    // group.html "Kurallar": a few short rules, not a handbook.
    public const int MaxRules = 10;
    public const int RuleTitle = 100;
    public const int RuleText = 500;

    // Announcements pinned to the top of the wall: more than a few and nothing stands out any more.
    public const int MaxPinnedPosts = 3;

    // The members list (group.html "Üyeler") is paged.
    public const int MaxMembersPageSize = 50;
}
