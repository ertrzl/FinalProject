using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class EventComment : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAnnouncement { get; set; }
}
