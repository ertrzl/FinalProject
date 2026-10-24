namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

public class PostCommentDto
{
    public Guid PostId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
