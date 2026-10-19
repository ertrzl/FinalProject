using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A game between two friends (games.html hub, game.html board). Starts as an invitation; once accepted the players
// are dealt X and O by chance and take turns, live over SignalR.
// Players are soft references (no FK, like Notification): the rows are removed with the account in UserRepository.
public class Game : BaseEntity
{
    public GameKind Kind { get; set; } = GameKind.TicTacToe;
    public GameStatus Status { get; set; } = GameStatus.Invited;

    public Guid InviterId { get; set; }
    public Guid InviteeId { get; set; }

    // The two ids in a fixed order, whoever invited whom, so one unique index can say "one open game per pair".
    public string PairKey { get; set; } = string.Empty;

    // Set when the invitation is accepted.
    public Guid? PlayerXId { get; set; }
    public Guid? PlayerOId { get; set; }

    // Nine cells row by row: 'X', 'O' or '-' for an empty one.
    public string Board { get; set; } = EmptyBoard;

    // Whose mark moves next ('X' or 'O'); X always starts.
    public char Turn { get; set; } = 'X';

    // Rock-paper-scissors only: what each side put out in the round being played (null: not picked yet; the other side
    // never sees it), and the scored rounds so far, two digits each (see RockPaperScissors.AppendRound).
    public RpsChoice? InviterChoice { get; set; }
    public RpsChoice? InviteeChoice { get; set; }
    public string Rounds { get; set; } = string.Empty;

    // Battleship only (see Application/Common/BattleshipGame.cs): each player's ships on a 100-character board ("" until the player has
    // placed them), and the shots each player fired at the other one ("" until the battle starts). Never sent to the other player.
    public string InviterBoard { get; set; } = string.Empty;
    public string InviteeBoard { get; set; } = string.Empty;
    public string InviterShots { get; set; } = string.Empty;
    public string InviteeShots { get; set; } = string.Empty;

    // Battleship only: the newest shot, so both pages can show it.
    public int? LastShotCell { get; set; }
    public ShotResult? LastShotResult { get; set; }
    public Guid? LastShotById { get; set; }

    public GameOutcome? Outcome { get; set; }
    public Guid? WinnerId { get; set; } // null for a draw
    public DateTime? FinishedAt { get; set; }

    // The last thing that happened (invited, accepted, a move...): an abandoned game is told apart by it.
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Two moves sent at the same moment: the second one is rejected instead of overwriting the first.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public const string EmptyBoard = "---------";

    public static string MakePairKey(Guid a, Guid b)
    {
        var first = a.ToString();
        var second = b.ToString();
        return string.CompareOrdinal(first, second) <= 0 ? $"{first}:{second}" : $"{second}:{first}";
    }
}
