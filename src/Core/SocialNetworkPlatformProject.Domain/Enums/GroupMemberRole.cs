namespace SocialNetworkPlatformProject.Domain.Enums;

public enum GroupMemberRole
{
    Member = 0,
    Admin = 1,
    // Appended after Admin so existing rows (stored as the int value) keep meaning unchanged.
    Moderator = 2
}
