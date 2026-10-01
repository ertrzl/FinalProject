namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// group.html's admin-only "Bekleyen İstekler" list.
public class GetJoinRequestDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime RequestedAt { get; set; }
}
