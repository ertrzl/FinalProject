namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Engellenenler" list: the people the current user has blocked.
public class GetBlockedUserDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime BlockedAt { get; set; }
}
