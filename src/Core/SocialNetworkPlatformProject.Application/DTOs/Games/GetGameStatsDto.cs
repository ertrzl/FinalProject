namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// The "Kazandın / Kaybettin / Berabere" chips on games.html: counted over all of a person's finished games.
public class GetGameStatsDto
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}
