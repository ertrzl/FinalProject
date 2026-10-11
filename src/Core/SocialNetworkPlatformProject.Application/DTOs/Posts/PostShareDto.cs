namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// "Gönderiyi paylaş": the text you add on top (optional) and who may see your share
public class PostShareDto
{
    public string? Text { get; set; }
    public string Privacy { get; set; } = "Public"; // "Public" or "FriendsOnly"
}
