namespace SocialNetworkPlatformProject.Application.DTOs.Games;

public class GetGamePlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
