using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Matches the composer form (home.html / profile.html): textarea + optional image + privacy select.
public class PostPostDto
{
    public string? Text { get; set; }
    public IFormFile? Image { get; set; }
    public string Privacy { get; set; } = "Public"; // "Public" or "FriendsOnly"
}
