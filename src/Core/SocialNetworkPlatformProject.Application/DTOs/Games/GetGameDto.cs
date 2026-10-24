namespace SocialNetworkPlatformProject.Application.DTOs.Games;

public class GetGameDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string Board { get; set; } = string.Empty;
    public string Turn { get; set; } = string.Empty;
    public string? MyMark { get; set; }
    public bool IsMyTurn { get; set; }

    public bool IsInvitedToMe { get; set; }
    public bool IsInvitedByMe { get; set; }

    public GetGamePlayerDto? PlayerX { get; set; }
    public GetGamePlayerDto? PlayerO { get; set; }
    public GetGamePlayerDto Inviter { get; set; } = new();
    public GetGamePlayerDto Invitee { get; set; } = new();
    public GetGamePlayerDto Opponent { get; set; } = new();

    public Guid? WinnerId { get; set; }
    public string? Outcome { get; set; }
    public int[]? WinningLine { get; set; }

    public string? MyChoice { get; set; }
    public bool OpponentHasChosen { get; set; }
    public List<GetGameRoundDto> Rounds { get; set; } = new();
    public int MyScore { get; set; }
    public int OpponentScore { get; set; }
    public int RoundNumber { get; set; }
    public int TotalRounds { get; set; }

    public string? Phase { get; set; }
    public bool IAmReady { get; set; }
    public bool OpponentReady { get; set; }
    public List<GetGameShipDto> MyShips { get; set; } = new();
    public List<GetGameShipDto> OpponentShips { get; set; } = new();
    public string MyShots { get; set; } = string.Empty;
    public string OpponentShots { get; set; } = string.Empty;
    public int MyShipsLeft { get; set; }
    public int OpponentShipsLeft { get; set; }
    public GetGameShotDto? LastShot { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
