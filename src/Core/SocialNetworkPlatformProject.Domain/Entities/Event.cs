using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// events.html cards + "Etkinlik Oluştur" modal
public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime StartsAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
}
