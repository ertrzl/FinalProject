using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// event.html's discussion section. An announcement is a comment the organizer pinned to the top; it also
// notifies everyone going or interested.
public class EventComment : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAnnouncement { get; set; }
}
