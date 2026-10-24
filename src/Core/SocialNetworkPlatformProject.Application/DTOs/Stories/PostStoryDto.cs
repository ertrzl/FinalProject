using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

public class PostStoryDto
{
    public IFormFile Media { get; set; } = null!;

    public List<Guid> TaggedUserIds { get; set; } = new();

    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }
}
