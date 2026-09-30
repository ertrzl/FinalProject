namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// groups.html "Gruplarım" / "Keşfet" cards
public class GetGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Privacy { get; set; } = string.Empty; // "Public" / "Private"
    public int MemberCount { get; set; }
    public bool IsCurrentUserMember { get; set; }
    public bool IsCurrentUserAdmin { get; set; }
    public bool IsCurrentUserModerator { get; set; }
    public bool IsCurrentUserOwner { get; set; }
    public DateTime CreatedAt { get; set; }

    // Only populated on the group-detail fetch (group.html), not on the mine/discover list cards.
    public List<GetGroupMemberDto> Members { get; set; } = new();
}
