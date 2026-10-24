namespace SocialNetworkPlatformProject.Application.Common;

public class UserSummary
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? CoverPhotoUrl { get; init; }
    public string? Bio { get; init; }
    public string? Location { get; init; }
    public string? Occupation { get; init; }
    public string? Education { get; init; }
    public bool IsPrivateAccount { get; init; }
    public bool ShowOnlineStatus { get; init; }
    public bool ShowFriendsList { get; init; }
    public bool IsSuspended { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
