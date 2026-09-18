using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// events.html "Katılıyorum" / "İlgileniyorum" buttons (setEventStatus() inline script)
public class EventAttendee : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public EventAttendeeStatus Status { get; set; }
}
