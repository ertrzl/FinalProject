using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// The original post shown inside a share (the framed box on the card)
public class GetSharedPostDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string? Text { get; set; }
    public List<GetPostMediaDto> Media { get; set; } = new();
    public List<GetMentionDto> Mentions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
