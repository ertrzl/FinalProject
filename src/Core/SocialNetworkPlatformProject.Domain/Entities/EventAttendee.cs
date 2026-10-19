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

    // When the person joined the waiting list (null otherwise): the order of the queue. Not the row's CreatedAt,
    // because someone who was "Interested" for a week and only now asks for a spot must queue from now.
    public DateTime? WaitlistedAt { get; set; }

    // The attendance list ("Yoklama"): when the organizer ticked this person as having come, and who ticked them. Only
    // someone who is going can have it; changing to any other status clears it again.
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInByUserId { get; set; }
}
