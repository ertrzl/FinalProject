using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// event.html "Fotoğraflar" tab: a picture added to an event's album by the organizer or by somebody going.
public class EventPhoto : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    // A soft reference (no foreign key, like EventComment.AuthorId): the account-deletion code removes these rows.
    public Guid UploadedByUserId { get; set; }

    public string Url { get; set; } = string.Empty;
}
