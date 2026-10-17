namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Gizlilik" section
public class PutPrivacySettingsDto
{
    public bool IsPrivateAccount { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public bool ShowFriendsList { get; set; }
}
