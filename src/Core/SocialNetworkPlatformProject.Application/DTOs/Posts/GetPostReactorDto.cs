namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// One row of the "who reacted" list
public class GetPostReactorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Type { get; set; } = string.Empty;
}
