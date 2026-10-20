namespace SocialNetworkPlatformProject.Application.Common;

// The server's rules for a group chat made on the Messages page (the pages only show them). The title length is also the database column's.
public static class ChatGroupLimits
{
    public const int TitleLength = 60;

    // The person who makes it picks at least this many other friends (a chat of two is a direct chat) and the whole chat holds at most MaxMembers.
    public const int MinOtherMembers = 2;
    public const int MaxMembers = 50;
}
