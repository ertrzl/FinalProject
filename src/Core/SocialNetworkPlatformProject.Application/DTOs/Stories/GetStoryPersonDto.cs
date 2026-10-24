namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

public class GetStoryPersonDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
