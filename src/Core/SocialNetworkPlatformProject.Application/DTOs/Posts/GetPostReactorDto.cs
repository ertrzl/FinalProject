namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class GetPostReactorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Type { get; set; } = string.Empty;
}
