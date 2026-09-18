namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

// Matches the comment bubble markup in home.html/profile.html + the reply thread (ParentCommentId).
public class GetCommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Guid? ParentCommentId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
