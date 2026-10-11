namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// "❤️ 12" under a post: how many people answered with this reaction
public class GetReactionCountDto
{
    public string Type { get; set; } = string.Empty; // Like / Love / Haha / Wow / Sad / Angry
    public int Count { get; set; }
}
