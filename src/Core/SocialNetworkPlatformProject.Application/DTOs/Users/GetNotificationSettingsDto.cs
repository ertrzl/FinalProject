namespace SocialNetworkPlatformProject.Application.DTOs.Users;

public class GetNotificationSettingsDto
{
    public bool Likes { get; set; }
    public bool Comments { get; set; }
    public bool FriendRequests { get; set; }
    public bool StoryTags { get; set; }
}
