namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// One person in a game (games.html, game.html)
public class GetGamePlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
