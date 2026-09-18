namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// F7: search.html results + navbar live search suggestions dropdown
public class GetUserSearchResultDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int MutualFriendsCount { get; set; }
    public string FriendshipStatus { get; set; } = "None";
}
