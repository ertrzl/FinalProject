namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class PostShareDto
{
    public string? Text { get; set; }
    public string Privacy { get; set; } = "Public";
}
