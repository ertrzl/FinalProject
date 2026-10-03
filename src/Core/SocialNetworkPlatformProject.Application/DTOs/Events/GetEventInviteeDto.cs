namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html's invite modal: who has already been invited (so they show "Davet edildi" instead of a button).
public class GetEventInviteeDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
}
