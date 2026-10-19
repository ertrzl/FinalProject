namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// home.html story bar + fullscreen viewer (js/stories.js)
public class GetStoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image"; // "Image" / "Video"
    // The song that plays with the story (all null when it has none).
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    // The friends tagged in the story (everybody who can see the story sees them).
    public List<GetStoryPersonDto> Tags { get; set; } = new();

    // A story added from somebody else's story: that person (null for an ordinary story, or when their account is gone).
    public GetStoryPersonDto? ResharedFrom { get; set; }

    // The person looking is tagged in this story, it is not theirs, and they have not added it to their own yet: "Hikayene ekle" is offered.
    public bool CanReshare { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Only filled in on your own stories: how many people have watched it (the list is GET /api/stories/{id}/viewers).
    public int ViewCount { get; set; }

    // Somebody else's story that you have already watched: its ring on the story bar is greyed out.
    public bool IsViewed { get; set; }

    // Only filled in on your own stories: how many people liked it (who liked it: HasLiked in the viewers list).
    public int LikeCount { get; set; }

    // Somebody else's story that you liked: its heart is filled.
    public bool IsLiked { get; set; }
}
