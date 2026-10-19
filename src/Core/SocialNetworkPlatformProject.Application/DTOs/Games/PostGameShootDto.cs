namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// A shot at the opponent's board: the cell counted row by row, 0 (top left) to 99 (bottom right) = row * 10 + column.
public class PostGameShootDto
{
    public int Cell { get; set; }
}
