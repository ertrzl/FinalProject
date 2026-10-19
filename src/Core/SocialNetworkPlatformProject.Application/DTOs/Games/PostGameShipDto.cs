namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// One ship of a battleship fleet as the player puts it on the board (also what GET api/games/random-fleet answers).
// Row and Col are the top-left cell of the ship: a horizontal ship covers Col..Col+length-1 of that row, a vertical one Row..Row+length-1 of that column.
public class PostGameShipDto
{
    public string Ship { get; set; } = string.Empty; // Carrier / Battleship / Cruiser / Submarine / Destroyer
    public int Row { get; set; }                     // 0 (top) to 9
    public int Col { get; set; }                     // 0 (left) to 9
    public bool Horizontal { get; set; }
}
