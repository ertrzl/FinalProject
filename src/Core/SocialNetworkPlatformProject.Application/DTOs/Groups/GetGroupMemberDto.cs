namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class GetGroupMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
}
