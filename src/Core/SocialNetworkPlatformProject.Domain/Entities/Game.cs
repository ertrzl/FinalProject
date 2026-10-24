using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Game : BaseEntity
{
    public GameKind Kind { get; set; } = GameKind.TicTacToe;
    public GameStatus Status { get; set; } = GameStatus.Invited;

    public Guid InviterId { get; set; }
    public Guid InviteeId { get; set; }

    public string PairKey { get; set; } = string.Empty;

    public Guid? PlayerXId { get; set; }
    public Guid? PlayerOId { get; set; }

    public string Board { get; set; } = EmptyBoard;

    public char Turn { get; set; } = 'X';

    public RpsChoice? InviterChoice { get; set; }
    public RpsChoice? InviteeChoice { get; set; }
    public string Rounds { get; set; } = string.Empty;

    public string InviterBoard { get; set; } = string.Empty;
    public string InviteeBoard { get; set; } = string.Empty;
    public string InviterShots { get; set; } = string.Empty;
    public string InviteeShots { get; set; } = string.Empty;

    public int? LastShotCell { get; set; }
    public ShotResult? LastShotResult { get; set; }
    public Guid? LastShotById { get; set; }

    public GameOutcome? Outcome { get; set; }
    public Guid? WinnerId { get; set; }
    public DateTime? FinishedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public const string EmptyBoard = "---------";

    public static string MakePairKey(Guid a, Guid b)
    {
        var first = a.ToString();
        var second = b.ToString();
        return string.CompareOrdinal(first, second) <= 0 ? $"{first}:{second}" : $"{second}:{first}";
    }
}
