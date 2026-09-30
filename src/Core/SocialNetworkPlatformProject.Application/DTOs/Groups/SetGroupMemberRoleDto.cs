namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class SetGroupMemberRoleDto
{
    public string Role { get; set; } = string.Empty; // "Admin" / "Moderator" / "Member"
}
