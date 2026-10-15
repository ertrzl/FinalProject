namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// The answer of POST api/auth/media-session: until when the photo cookie that was just set is good.
public class MediaSessionDto
{
    public DateTime ExpiresAt { get; set; }
}
