namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html's creator-only "Etkinliği Düzenle" modal. A full replace: sending no Capacity removes the limit.
public class PutEventDto : EventInputDto
{
    public bool RemoveCoverImage { get; set; }
}
