namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Editing an existing post — text/privacy only (the image isn't re-uploaded on edit).
public class PutPostDto
{
    public string? Text { get; set; }
    public string Privacy { get; set; } = "Public";
}
