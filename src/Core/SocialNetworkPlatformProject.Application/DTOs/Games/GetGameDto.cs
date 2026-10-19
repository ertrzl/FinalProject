namespace SocialNetworkPlatformProject.Application.DTOs.Games;

// One game as the person asking sees it (games.html hub, game.html board, and the "GameUpdated" push).
// Built per viewer: MyMark, IsMyTurn, Opponent... differ between the two players of the same game.
public class GetGameDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;   // "TicTacToe" / "RockPaperScissors"
    public string Status { get; set; } = string.Empty; // Invited / Active / Finished / Declined / Cancelled / Expired

    public string Board { get; set; } = string.Empty;  // nine characters row by row: X, O or - (empty)
    public string Turn { get; set; } = string.Empty;   // "X" or "O": whose mark moves next
    public string? MyMark { get; set; }                 // null only for somebody who is not dealt a mark yet (not accepted)
    public bool IsMyTurn { get; set; }                  // only true while the game is Active

    public bool IsInvitedToMe { get; set; }             // Invited, and I am the one who has to answer
    public bool IsInvitedByMe { get; set; }             // Invited, and I am the one waiting for the answer

    public GetGamePlayerDto? PlayerX { get; set; }      // null until the invitation is accepted
    public GetGamePlayerDto? PlayerO { get; set; }
    public GetGamePlayerDto Inviter { get; set; } = new();
    public GetGamePlayerDto Invitee { get; set; } = new();
    public GetGamePlayerDto Opponent { get; set; } = new(); // the other person, from my point of view

    public Guid? WinnerId { get; set; }                 // null for a draw (and while the game runs)
    public string? Outcome { get; set; }                // Win / Draw / Resigned
    public int[]? WinningLine { get; set; }             // the three winning cells, only for a Win

    // Rock-paper-scissors only (empty/zero for X-O). The opponent's pick of the running round is never sent.
    public string? MyChoice { get; set; }               // Rock / Paper / Scissors: my pick of the round being played
    public bool OpponentHasChosen { get; set; }         // the opponent has picked already (what they picked stays secret)
    public List<GetGameRoundDto> Rounds { get; set; } = new(); // the scored rounds, results from my point of view
    public int MyScore { get; set; }                    // round wins
    public int OpponentScore { get; set; }
    public int RoundNumber { get; set; }                // the round being played (1..3); when Finished, how many were played
    public int TotalRounds { get; set; }                // 3 for rock-paper-scissors, 0 for X-O

    // Battleship only (empty/null for the other kinds). The opponent's ships are never in here, except the ones I sank (all of them once the game is over).
    public string? Phase { get; set; }                  // Placing / Battle; null when the game is not running
    public bool IAmReady { get; set; }                  // I placed my fleet
    public bool OpponentReady { get; set; }
    public List<GetGameShipDto> MyShips { get; set; } = new();       // my fleet as placed (empty before I placed)
    public List<GetGameShipDto> OpponentShips { get; set; } = new(); // opponent ships I sank; all of them when Finished
    public string MyShots { get; set; } = string.Empty;       // 100 chars: - not shot, o miss, x hit (what I fired)
    public string OpponentShots { get; set; } = string.Empty; // the same, what the opponent fired at me
    public int MyShipsLeft { get; set; }
    public int OpponentShipsLeft { get; set; }
    public GetGameShotDto? LastShot { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
