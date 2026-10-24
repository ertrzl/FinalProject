namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

public class GetStoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image";
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    public List<GetStoryPersonDto> Tags { get; set; } = new();

    public GetStoryPersonDto? ResharedFrom { get; set; }

    public bool CanReshare { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public int ViewCount { get; set; }

    public bool IsViewed { get; set; }

    public int LikeCount { get; set; }

    public bool IsLiked { get; set; }
}
