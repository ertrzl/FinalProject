namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html "Davetler" tab: one pending invite addressed to the current user.
public class GetEventInviteDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsOnline { get; set; }
    public string? Location { get; set; }
    public string? GroupName { get; set; }

    public Guid InvitedByUserId { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public string? InvitedByAvatarUrl { get; set; }
    public DateTime InvitedAt { get; set; }
}
