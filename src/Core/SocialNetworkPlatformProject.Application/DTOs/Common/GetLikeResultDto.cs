namespace SocialNetworkPlatformProject.Application.DTOs.Common;

// Returned by like toggles on posts and comments.
public class GetLikeResultDto
{
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
}
