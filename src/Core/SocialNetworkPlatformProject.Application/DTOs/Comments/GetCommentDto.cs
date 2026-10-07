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

    // The comment's author, the post's author, or (on a group post) an admin/moderator of that group.
    public bool CanDelete { get; set; }
}
