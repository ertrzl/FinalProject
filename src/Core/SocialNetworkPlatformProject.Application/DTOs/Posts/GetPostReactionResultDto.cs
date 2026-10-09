namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// What the card shows after somebody reacts: your reaction now, the total and the split by reaction.
public class GetPostReactionResultDto
{
    public string? MyReaction { get; set; } // null = none
    public bool IsLiked { get; set; }       // any reaction counts as a "like" for the heart button
    public int LikeCount { get; set; }      // all reactions together
    public List<GetReactionCountDto> Reactions { get; set; } = new(); // biggest first
}
