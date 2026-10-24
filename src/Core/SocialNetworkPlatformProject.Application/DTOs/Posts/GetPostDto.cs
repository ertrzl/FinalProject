using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class GetPostDto
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }

    public string? Text { get; set; }
    public List<GetPostMediaDto> Media { get; set; } = new();
    public List<GetMentionDto> Mentions { get; set; } = new();
    public string Privacy { get; set; } = string.Empty;

    public bool IsGroupPost { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Hashtags { get; set; } = new();

    public int LikeCount { get; set; }
    public List<GetReactionCountDto> Reactions { get; set; } = new();
    public string? MyReaction { get; set; }
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }

    public bool IsShare { get; set; }
    public GetSharedPostDto? SharedPost { get; set; }

    public bool IsEventShare { get; set; }
    public GetSharedEventDto? SharedEvent { get; set; }

    public GetPollDto? Poll { get; set; }
    public bool IsSavedByCurrentUser { get; set; }

    public bool CanDelete { get; set; }

    public bool CanEdit { get; set; }
}
