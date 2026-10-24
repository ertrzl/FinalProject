namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class GetGroupInviteDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupCoverImageUrl { get; set; }
    public string? GroupAvatarUrl { get; set; }
    public int GroupRuleCount { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
