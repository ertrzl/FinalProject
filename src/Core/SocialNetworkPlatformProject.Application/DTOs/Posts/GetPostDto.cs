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
    public string? ImageUrl { get; set; }
    public string Privacy { get; set; } = string.Empty; // "Public" / "FriendsOnly"
    public DateTime CreatedAt { get; set; }

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsSavedByCurrentUser { get; set; }
}
