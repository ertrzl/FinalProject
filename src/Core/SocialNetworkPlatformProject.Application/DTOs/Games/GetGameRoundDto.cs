namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// One scored round of rock-paper-scissors, from the point of view of the person asking.
public class GetGameRoundDto
{
    public string MyChoice { get; set; } = string.Empty;       // Rock / Paper / Scissors
    public string OpponentChoice { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;         // Win / Loss / Draw
}
