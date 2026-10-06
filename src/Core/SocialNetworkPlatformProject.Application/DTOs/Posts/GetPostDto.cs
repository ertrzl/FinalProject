namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Shape matches what home.html / profile.html's feed rendering expects
// (see the post card markup and js/app.js's publishPost()/toggleLike()).
public class GetPostDto
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }

    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public string MediaType { get; set; } = "Image"; // "Image" / "Video"
    public string Privacy { get; set; } = string.Empty; // "Public" / "FriendsOnly"
    public DateTime CreatedAt { get; set; }
    public List<string> Hashtags { get; set; } = new();

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsSavedByCurrentUser { get; set; }

    // The author, or (on a group post) an admin/moderator of that group. The server decides; the page only shows the button.
    public bool CanDelete { get; set; }
}
