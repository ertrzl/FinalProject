namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

// Matches the comment-form in home.html (addComment() in app.js) — also used for one-level replies.
public class PostCommentDto
{
    public Guid PostId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
