namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// groups.html "Davetler" tab — invites waiting for the current user to accept/decline.
public class GetGroupInviteDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupCoverImageUrl { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
