namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// One photo/video of a post card (js/api.js postMediaHtml)
public class GetPostMediaDto
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; // "Image" / "Video"
}
