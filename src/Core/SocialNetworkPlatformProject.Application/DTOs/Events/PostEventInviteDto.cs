namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html's "Davet Et" modal: invite one person (any real user, not just friends).
public class PostEventInviteDto
{
    public Guid UserId { get; set; }
}
