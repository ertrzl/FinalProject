namespace SocialNetworkPlatformProject.Application.DTOs.Games;

public class PostGameShipDto
{
    public string Ship { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Col { get; set; }
    public bool Horizontal { get; set; }
}
