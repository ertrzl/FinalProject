namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// The emoji picked under a post's "Beğen" button; null or empty takes the reaction back.
public class PutReactionDto
{
    public string? Type { get; set; }
}
