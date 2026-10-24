namespace SocialNetworkPlatformProject.Application.DTOs.Users;

public class GetUserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public DateTime JoinedAt { get; set; }

    public int FriendCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsPrivateAccount { get; set; }

    public bool ShowOnlineStatus { get; set; }
    public bool ShowFriendsList { get; set; }

    public bool IsOwnProfile { get; set; }

    public bool IsBlockedByMe { get; set; }
    public string FriendshipStatus { get; set; } = "None";

    public bool CanMessage { get; set; }
}
