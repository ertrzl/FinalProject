namespace SocialNetworkPlatformProject.Application.DTOs.Games;

public class GetGameShotDto
{
    public int Cell { get; set; }
    public string Result { get; set; } = string.Empty;
    public bool ByMe { get; set; }
}
