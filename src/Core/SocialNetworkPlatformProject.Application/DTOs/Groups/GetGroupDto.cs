namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class GetGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public string Privacy { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public bool IsMutedByCurrentUser { get; set; }
    public int MemberCount { get; set; }
    public bool IsCurrentUserMember { get; set; }
    public bool IsCurrentUserAdmin { get; set; }
    public bool IsCurrentUserModerator { get; set; }
    public bool IsCurrentUserOwner { get; set; }
    public bool HasPendingJoinRequest { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<GetGroupMemberDto> Members { get; set; } = new();
}
