namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class PostEventDto : EventInputDto
{
    public Guid? GroupId { get; set; }
}
