using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Matches the composer form (home.html / profile.html): textarea + optional photos/video + privacy select.
public class PostPostDto
{
    public string? Text { get; set; }

    // Up to PostLimits.MaxImages photos, or one video (the form field "media" is repeated, one per file).
    public List<IFormFile> Media { get; set; } = new();
    public string Privacy { get; set; } = "Public"; // "Public" or "FriendsOnly" — ignored when GroupId is set

    // Set by group.html's composer to post on a group's wall instead of the author's own feed.
    public Guid? GroupId { get; set; }
}
