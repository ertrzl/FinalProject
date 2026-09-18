namespace SocialNetworkPlatformProject.Application.DTOs.Friends;

// friends.html "Arkadaşlarım" tab, profile.html friend grid
public class GetFriendDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Location { get; set; }
    public DateTime FriendsSince { get; set; }
}
