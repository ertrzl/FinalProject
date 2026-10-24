using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class EventAttendee : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public EventAttendeeStatus Status { get; set; }

    public DateTime? WaitlistedAt { get; set; }

    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInByUserId { get; set; }
}
