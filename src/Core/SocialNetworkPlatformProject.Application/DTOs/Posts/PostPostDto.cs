using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Matches the composer form (home.html / profile.html): textarea + optional image + privacy select.
public class PostPostDto
{
    public string? Text { get; set; }
    public IFormFile? Media { get; set; } // image or short video
    public string Privacy { get; set; } = "Public"; // "Public" or "FriendsOnly" — ignored when GroupId is set

    // Set by group.html's composer to post on a group's wall instead of the author's own feed.
    public Guid? GroupId { get; set; }
}
