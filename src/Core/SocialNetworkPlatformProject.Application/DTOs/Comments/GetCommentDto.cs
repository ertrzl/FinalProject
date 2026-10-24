using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

public class GetCommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }

    public string Text { get; set; } = string.Empty;
    public List<GetMentionDto> Mentions { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public Guid? ParentCommentId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }

    public bool CanDelete { get; set; }
}
