namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// Inviting a friend to a game ("Davet et" on games.html, "Tekrar oyna" on game.html)
public class PostGameDto
{
    public Guid OpponentId { get; set; }

    // "TicTacToe"; left out means the only game there is today.
    public string? Kind { get; set; }
}
