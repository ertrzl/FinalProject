namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class PutPostDto
{
    public string? Text { get; set; }
    public string Privacy { get; set; } = "Public";
}
