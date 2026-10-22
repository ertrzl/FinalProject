namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Bildirim Tercihleri": true = the user wants these notifications.
public class GetNotificationSettingsDto
{
    public bool Likes { get; set; }
    public bool Comments { get; set; }
    public bool FriendRequests { get; set; }
    public bool StoryTags { get; set; } // tagged in a story / a story one is in was shared again
}
