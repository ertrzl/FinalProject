namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class GetReactionCountDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}
