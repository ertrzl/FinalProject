using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class EventInvite : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid InvitedUserId { get; set; }
    public Guid InvitedByUserId { get; set; }
}
