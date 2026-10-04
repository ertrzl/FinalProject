using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// The fields an organizer fills in, shared by "Etkinlik Oluştur" (create) and "Etkinliği Düzenle" (update).
public abstract class EventInputDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }

    // Moments in time: the browser sends them with an offset ("...Z"); EventService normalizes them to UTC.
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; } // null = start + EventLimits.DefaultDuration

    public IFormFile? CoverImage { get; set; }

    // Only the organizer, attendees and invited people can see it; only the organizer can invite. Not for group events.
    public bool IsPrivate { get; set; }

    // Optional limit on how many people can say "Katılıyorum". Null = no limit.
    public int? Capacity { get; set; }

    // Online events have a link instead of a place; the link is only shown to the organizer and people who are going.
    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }
}
