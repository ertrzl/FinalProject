using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// "Hikaye Ekle" modal on home.html — accepts either a photo or a short video.
public class PostStoryDto
{
    public IFormFile Media { get; set; } = null!;

    // Optional friends to tag (the form field "taggedUserIds" is repeated, one per friend). Friends only, at most 5.
    public List<Guid> TaggedUserIds { get; set; } = new();

    // Optional song chosen with "Müzik ekle" (what GET /api/music/search answered). The address must be one of Apple's preview links.
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }
}
