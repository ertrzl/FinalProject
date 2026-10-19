namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// A ship on a battleship board, with where it lies and whether it is sunk.
public class GetGameShipDto
{
    public string Ship { get; set; } = string.Empty; // Carrier / Battleship / Cruiser / Submarine / Destroyer
    public int Row { get; set; }
    public int Col { get; set; }
    public bool Horizontal { get; set; }
    public int Length { get; set; }
    public bool Sunk { get; set; }
}
