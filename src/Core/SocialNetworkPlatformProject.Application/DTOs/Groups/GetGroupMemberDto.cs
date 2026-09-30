namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// group.html "Üyeler" list.
public class GetGroupMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty; // "Admin" / "Moderator" / "Member"
    public bool IsOwner { get; set; }
}
