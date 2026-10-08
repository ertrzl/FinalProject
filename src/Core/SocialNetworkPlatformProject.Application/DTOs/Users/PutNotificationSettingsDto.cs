namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Bildirim Tercihleri" "Kaydet": the three switches as the user left them.
public class PutNotificationSettingsDto
{
    public bool Likes { get; set; }
    public bool Comments { get; set; }
    public bool FriendRequests { get; set; }
}
