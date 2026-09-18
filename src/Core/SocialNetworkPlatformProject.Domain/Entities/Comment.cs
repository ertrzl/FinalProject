using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F5: threaded comments (home.html/profile.html ".comment.reply" — one level of nesting via ParentCommentId)
public class Comment : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
}
