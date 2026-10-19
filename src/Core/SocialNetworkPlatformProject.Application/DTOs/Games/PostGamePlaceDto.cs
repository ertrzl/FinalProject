namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// "Hazırım": the whole fleet, one of each ship, sent once.
public class PostGamePlaceDto
{
    public List<PostGameShipDto> Ships { get; set; } = new();
}
