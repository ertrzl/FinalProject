namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventInviteeDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
}
