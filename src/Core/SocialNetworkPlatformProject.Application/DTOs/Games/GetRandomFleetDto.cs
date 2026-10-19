namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// A random valid fleet layout for the "Rastgele diz" button (not saved anywhere).
public class GetRandomFleetDto
{
    public List<PostGameShipDto> Ships { get; set; } = new();
}
