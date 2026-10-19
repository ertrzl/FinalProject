namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// The newest shot of a battleship game, for a short highlight on both pages.
public class GetGameShotDto
{
    public int Cell { get; set; }
    public string Result { get; set; } = string.Empty; // Miss / Hit / Sunk
    public bool ByMe { get; set; }
}
