namespace SocialNetworkPlatformProject.Application.DTOs.Games;

public class GetGamesOverviewDto
{
    public List<GetGameDto> IncomingInvites { get; set; } = new();
    public List<GetGameDto> SentInvites { get; set; } = new();
    public List<GetGameDto> Active { get; set; } = new();
    public List<GetGameDto> Recent { get; set; } = new();
    public GetGameStatsDto Stats { get; set; } = new();
}
