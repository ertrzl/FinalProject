namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class GetPostReactionResultDto
{
    public string? MyReaction { get; set; }
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
    public List<GetReactionCountDto> Reactions { get; set; } = new();
}
