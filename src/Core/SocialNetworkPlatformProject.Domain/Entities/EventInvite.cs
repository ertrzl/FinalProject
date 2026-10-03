using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// "X seni davet etti": events.html's "Davetler" tab. Deleted when the invitee answers (joining creates an
// EventAttendee row instead) or when the event goes away.
public class EventInvite : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid InvitedUserId { get; set; }
    public Guid InvitedByUserId { get; set; } // the organizer, or someone who is going
}
