namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// Giving somebody the right to run a group chat, or taking it back.
public class PutChatGroupAdminDto
{
    public bool IsAdmin { get; set; }
}
