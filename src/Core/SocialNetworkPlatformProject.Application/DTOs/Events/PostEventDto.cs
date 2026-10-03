namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// "Etkinlik Oluştur" modal on events.html
public class PostEventDto : EventInputDto
{
    // Set to make a group event: only that group's members can see it, and only its admins/moderators may create one.
    public Guid? GroupId { get; set; }
}
