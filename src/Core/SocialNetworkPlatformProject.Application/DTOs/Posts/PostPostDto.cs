using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class PostPostDto
{
    public string? Text { get; set; }

    public List<IFormFile> Media { get; set; } = new();
    public List<string> PollOptions { get; set; } = new();
    public int? PollDurationHours { get; set; }

    public string Privacy { get; set; } = "Public";

    public Guid? GroupId { get; set; }
}
