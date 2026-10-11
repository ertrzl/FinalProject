namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// explore.html "Gündemdeki etiketler"
public class GetTrendingHashtagDto
{
    public string Name { get; set; } = string.Empty; // lower-case, without the "#"
    public int PostCount { get; set; }
}
