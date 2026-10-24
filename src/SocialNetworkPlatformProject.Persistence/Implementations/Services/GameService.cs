using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class GameService : IGameService
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan IdleLimit = TimeSpan.FromDays(7);

    private const int RecentGamesListed = 20;
    private const string UpdatedEvent = "GameUpdated";

    private readonly IGameRepository _games;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly INotificationService _notifications;
    private readonly IRealTimeNotifier _notifier;

    public GameService(
        IGameRepository games,
        IUserRepository users,
        IFriendService friends,
        IBlockService blocks,
        INotificationService notifications,
        IRealTimeNotifier notifier)
    {
        _games = games;
        _users = users;
        _friends = friends;
        _blocks = blocks;
        _notifications = notifications;
        _notifier = notifier;
    }

    public async Task<GetGamesOverviewDto> GetOverviewAsync(Guid currentUserId)
    {
        var now = DateTime.UtcNow;
        var incoming = await _games.GetIncomingInvitesAsync(currentUserId, now - InviteLifetime);
        var sent = await _games.GetSentInvitesAsync(currentUserId, now - InviteLifetime);
        var active = await _games.GetActiveAsync(currentUserId, now - IdleLimit);
        var recent = await _games.GetRecentFinishedAsync(currentUserId, RecentGamesListed);

        var people = await LoadPeopleAsync(incoming.Concat(sent).Concat(active).Concat(recent));

        return new GetGamesOverviewDto
        {
            IncomingInvites = incoming.Select(g => ToDto(g, currentUserId, people)).ToList(),
            SentInvites = sent.Select(g => ToDto(g, currentUserId, people)).ToList(),
            Active = active.Select(g => ToDto(g, currentUserId, people)).ToList(),
            Recent = recent.Select(g => ToDto(g, currentUserId, people)).ToList(),
            Stats = await _games.GetStatsAsync(currentUserId)
        };
    }

    public async Task<GetGameDto> GetAsync(Guid currentUserId, Guid gameId)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        var people = await LoadPeopleAsync(new[] { game });
        return ToDto(game, currentUserId, people);
    }

    public async Task<GetGameDto> InviteAsync(Guid currentUserId, PostGameDto dto)
    {
        if (dto.OpponentId == currentUserId)
            throw new BadRequestException(Loc.T("game.self"));

        var kind = GameKind.TicTacToe;
        if (!string.IsNullOrWhiteSpace(dto.Kind) && !(Enum.TryParse(dto.Kind, true, out kind) && Enum.IsDefined(kind)))
            throw new BadRequestException(Loc.T("game.kindUnknown"));

        if (!await _users.ExistsAsync(dto.OpponentId))
            throw new NotFoundException(Loc.T("user.notFound"));

        if (await _blocks.AreBlockedAsync(currentUserId, dto.OpponentId))
            throw new ForbiddenException(Loc.T("game.cannot"));

        if (!await _friends.AreFriendsAsync(currentUserId, dto.OpponentId))
            throw new ForbiddenException(Loc.T("game.friendsOnly"));

        var pairKey = Game.MakePairKey(currentUserId, dto.OpponentId);
        var now = DateTime.UtcNow;
        await _games.ExpireStaleForPairAsync(pairKey, now - InviteLifetime, now - IdleLimit, now);

        if (await _games.AnyAsync(g => g.PairKey == pairKey && (g.Status == GameStatus.Invited || g.Status == GameStatus.Active)))
            throw new BadRequestException(Loc.T("game.alreadyPlaying"));

        var game = new Game
        {
            Kind = kind,
            InviterId = currentUserId,
            InviteeId = dto.OpponentId,
            PairKey = pairKey,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _games.AddAsync(game);
        try
        {
            await _games.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            throw new BadRequestException(Loc.T("game.alreadyPlaying"));
        }

        await _notifications.CreateAsync(dto.OpponentId, currentUserId, NotificationType.GameInviteReceived, gameId: game.Id);

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> AcceptAsync(Guid currentUserId, Guid gameId)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.InviteeId != currentUserId)
            throw new ForbiddenException(Loc.T("game.notInvitee"));

        EnsureInvitationOpen(game);
        await EnsureNotBlockedAsync(game, currentUserId);

        if (game.Kind == GameKind.TicTacToe || game.Kind == GameKind.Battleship)
        {
            var inviterIsX = Random.Shared.Next(2) == 0;
            game.PlayerXId = inviterIsX ? game.InviterId : game.InviteeId;
            game.PlayerOId = inviterIsX ? game.InviteeId : game.InviterId;
            game.Turn = 'X';
        }
        game.Status = GameStatus.Active;
        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        await _notifications.CreateAsync(game.InviterId, currentUserId, NotificationType.GameInviteAccepted, gameId: game.Id);

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> DeclineAsync(Guid currentUserId, Guid gameId)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.InviteeId != currentUserId)
            throw new ForbiddenException(Loc.T("game.notInvitee"));

        EnsureInvitationOpen(game);

        game.Status = GameStatus.Declined;
        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> CancelAsync(Guid currentUserId, Guid gameId)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.InviterId != currentUserId)
            throw new ForbiddenException(Loc.T("game.notInviter"));

        EnsureInvitationOpen(game);

        game.Status = GameStatus.Cancelled;
        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> MoveAsync(Guid currentUserId, Guid gameId, PostGameMoveDto dto)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.Kind != GameKind.TicTacToe)
            throw new BadRequestException(Loc.T("game.wrongKind"));

        if (game.Status != GameStatus.Active)
            throw new BadRequestException(Loc.T("game.notActive"));

        await EnsureNotBlockedAsync(game, currentUserId);

        var mark =game.PlayerXId == currentUserId ? 'X' : 'O';
        if (game.Turn != mark)
            throw new BadRequestException(Loc.T("game.notYourTurn"));

        if (dto.Cell < 0 || dto.Cell >= game.Board.Length)
            throw new BadRequestException(Loc.T("game.cellInvalid"));

        if (game.Board[dto.Cell] != TicTacToe.Empty)
            throw new BadRequestException(Loc.T("game.cellTaken"));

        var cells = game.Board.ToCharArray();
        cells[dto.Cell] = mark;
        game.Board = new string(cells);

        if (TicTacToe.FindWinningLine(game.Board) != null)
            Finish(game, GameOutcome.Win, currentUserId);
        else if (TicTacToe.IsFull(game.Board))
            Finish(game, GameOutcome.Draw, null);
        else
            game.Turn = mark == 'X' ? 'O' : 'X';

        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> ChooseAsync(Guid currentUserId, Guid gameId, PostGameChoiceDto dto)
    {
        if (!Enum.TryParse<RpsChoice>(dto.Choice, true, out var choice) || !Enum.IsDefined(choice))
            throw new BadRequestException(Loc.T("val.game.choice"));

        var game = await FindAsPlayerAsync(currentUserId, gameId);

        for (var attempt = 0; ; attempt++)
        {
            if (game.Kind != GameKind.RockPaperScissors)
                throw new BadRequestException(Loc.T("game.wrongKind"));

            if (game.Status != GameStatus.Active)
                throw new BadRequestException(Loc.T("game.notActive"));

            await EnsureNotBlockedAsync(game, currentUserId);

            var isInviter = game.InviterId == currentUserId;
            if ((isInviter ? game.InviterChoice : game.InviteeChoice) != null)
                throw new BadRequestException(Loc.T("game.alreadyChose"));

            if (isInviter) game.InviterChoice = choice;
            else game.InviteeChoice = choice;

            if (game.InviterChoice != null && game.InviteeChoice != null)
                ScoreRound(game);

            game.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _games.SaveChangesAsync();
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 3)
            {
                await _games.ReloadAsync(game);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BadRequestException(Loc.T("server.changedMeanwhile"));
            }
        }

        return await PublishAsync(game, currentUserId);
    }

    private static void ScoreRound(Game game)
    {
        game.Rounds = RockPaperScissors.AppendRound(game.Rounds, game.InviterChoice!.Value, game.InviteeChoice!.Value);
        game.InviterChoice = null;
        game.InviteeChoice = null;

        var (inviterWins, inviteeWins, played) = RockPaperScissors.Score(game.Rounds);
        if (!RockPaperScissors.IsOver(inviterWins, inviteeWins, played))
            return;

        if (inviterWins == inviteeWins)
            Finish(game, GameOutcome.Draw, null);
        else
            Finish(game, GameOutcome.Win, inviterWins > inviteeWins ? game.InviterId : game.InviteeId);
    }

    public GetRandomFleetDto GetRandomFleet()
    {
        return new GetRandomFleetDto
        {
            Ships = BattleshipGame.RandomFleet(Random.Shared)
                .Select(p => new PostGameShipDto { Ship = p.Kind.ToString(), Row = p.Row, Col = p.Col, Horizontal = p.Horizontal })
                .ToList()
        };
    }

    public async Task<GetGameDto> PlaceAsync(Guid currentUserId, Guid gameId, PostGamePlaceDto dto)
    {
        var placements = dto.Ships
            .Select(s => new BattleshipGame.Placement(Enum.Parse<ShipKind>(s.Ship, true), s.Row, s.Col, s.Horizontal))
            .ToList();
        if (!BattleshipGame.TryBuildBoard(placements, out var board))
            throw new BadRequestException(Loc.T("game.shipsInvalid"));

        var game = await FindAsPlayerAsync(currentUserId, gameId);

        for (var attempt = 0; ; attempt++)
        {
            if (game.Kind != GameKind.Battleship)
                throw new BadRequestException(Loc.T("game.wrongKind"));

            if (game.Status != GameStatus.Active)
                throw new BadRequestException(Loc.T("game.notActive"));

            await EnsureNotBlockedAsync(game, currentUserId);

            var isInviter = game.InviterId == currentUserId;
            if ((isInviter ? game.InviterBoard : game.InviteeBoard).Length > 0)
                throw new BadRequestException(Loc.T("game.alreadyPlaced"));

            if (isInviter) game.InviterBoard = board;
            else game.InviteeBoard = board;

            if (game.InviterBoard.Length > 0 && game.InviteeBoard.Length > 0)
            {
                game.InviterShots = BattleshipGame.EmptyShots();
                game.InviteeShots = BattleshipGame.EmptyShots();
                game.Turn = 'X';
            }

            game.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _games.SaveChangesAsync();
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 3)
            {
                await _games.ReloadAsync(game);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BadRequestException(Loc.T("server.changedMeanwhile"));
            }
        }

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> ShootAsync(Guid currentUserId, Guid gameId, PostGameShootDto dto)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.Kind != GameKind.Battleship)
            throw new BadRequestException(Loc.T("game.wrongKind"));

        if (game.Status != GameStatus.Active)
            throw new BadRequestException(Loc.T("game.notActive"));

        if (game.InviterBoard.Length == 0 || game.InviteeBoard.Length == 0)
            throw new BadRequestException(Loc.T("game.notBattle"));

        await EnsureNotBlockedAsync(game, currentUserId);

        var mark = game.PlayerXId == currentUserId ? 'X' : 'O';
        if (game.Turn != mark)
            throw new BadRequestException(Loc.T("game.notYourTurn"));

        if (dto.Cell < 0 || dto.Cell >= BattleshipGame.Cells)
            throw new BadRequestException(Loc.T("game.cellInvalid"));

        var isInviter = game.InviterId == currentUserId;
        var targetBoard = isInviter ? game.InviteeBoard : game.InviterBoard;
        var shots = (isInviter ? game.InviterShots : game.InviteeShots).ToCharArray();

        if (shots[dto.Cell] != BattleshipGame.NotShot)
            throw new BadRequestException(Loc.T("game.shotTaken"));

        var hit = targetBoard[dto.Cell] != BattleshipGame.Water;
        shots[dto.Cell] = hit ? BattleshipGame.Hit : BattleshipGame.Miss;
        var shotsAfter = new string(shots);
        if (isInviter) game.InviterShots = shotsAfter;
        else game.InviteeShots = shotsAfter;

        var result = !hit ? ShotResult.Miss
            : BattleshipGame.IsSunk(targetBoard, shotsAfter, targetBoard[dto.Cell]) ? ShotResult.Sunk
            : ShotResult.Hit;
        game.LastShotCell = dto.Cell;
        game.LastShotResult = result;
        game.LastShotById = currentUserId;

        if (BattleshipGame.AllSunk(targetBoard, shotsAfter))
            Finish(game, GameOutcome.Win, currentUserId);
        else if (!hit)
            game.Turn = mark == 'X' ? 'O' : 'X';

        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        return await PublishAsync(game, currentUserId);
    }

    public async Task<GetGameDto> ResignAsync(Guid currentUserId, Guid gameId)
    {
        var game = await FindAsPlayerAsync(currentUserId, gameId);
        if (game.Status != GameStatus.Active)
            throw new BadRequestException(Loc.T("game.notActive"));

        var otherPlayerId = game.InviterId == currentUserId ? game.InviteeId : game.InviterId;
        Finish(game, GameOutcome.Resigned, otherPlayerId);
        game.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();

        return await PublishAsync(game, currentUserId);
    }

    private async Task<Game> FindAsPlayerAsync(Guid currentUserId, Guid gameId)
    {
        var game = await _games.GetByIdAsync(gameId);
        if (game == null || (game.InviterId != currentUserId && game.InviteeId != currentUserId))
            throw new NotFoundException(Loc.T("game.notFound"));

        var now = DateTime.UtcNow;
        var runOut = (game.Status == GameStatus.Invited && game.CreatedAt < now - InviteLifetime)
            || (game.Status == GameStatus.Active && game.UpdatedAt < now - IdleLimit);
        if (runOut)
        {
            game.Status = GameStatus.Expired;
            game.UpdatedAt = now;
            await SaveAsync();
        }

        return game;
    }

    private static void EnsureInvitationOpen(Game game)
    {
        if (game.Status == GameStatus.Expired)
            throw new BadRequestException(Loc.T("game.expired"));

        if (game.Status != GameStatus.Invited)
            throw new BadRequestException(Loc.T("game.notInvited"));
    }

    private async Task EnsureNotBlockedAsync(Game game, Guid currentUserId)
    {
        var opponentId = game.InviterId == currentUserId ? game.InviteeId : game.InviterId;
        if (await _blocks.AreBlockedAsync(currentUserId, opponentId))
            throw new ForbiddenException(Loc.T("game.cannot"));
    }

    private static void Finish(Game game, GameOutcome outcome, Guid? winnerId)
    {
        game.Status = GameStatus.Finished;
        game.Outcome = outcome;
        game.WinnerId = winnerId;
        game.FinishedAt = DateTime.UtcNow;
    }

    private async Task SaveAsync()
    {
        try
        {
            await _games.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BadRequestException(Loc.T("server.changedMeanwhile"));
        }
    }

    private async Task<GetGameDto> PublishAsync(Game game, Guid currentUserId)
    {
        var people = await LoadPeopleAsync(new[] { game });

        await _notifier.PublishToMessagesAsync(game.InviterId, UpdatedEvent, ToDto(game, game.InviterId, people));
        await _notifier.PublishToMessagesAsync(game.InviteeId, UpdatedEvent, ToDto(game, game.InviteeId, people));

        return ToDto(game, currentUserId, people);
    }

    private async Task<Dictionary<Guid, UserSummary>> LoadPeopleAsync(IEnumerable<Game> games)
    {
        var ids = games.SelectMany(g => new[] { g.InviterId, g.InviteeId }).Distinct();
        return await _users.GetSummariesAsync(ids);
    }

    private static GetGameDto ToDto(Game game, Guid viewerId, Dictionary<Guid, UserSummary> people)
    {
        var myMark = game.PlayerXId == viewerId ? "X" : game.PlayerOId == viewerId ? "O" : null;
        var opponentId = game.InviterId == viewerId ? game.InviteeId : game.InviterId;
        var invited = game.Status == GameStatus.Invited;
        var active = game.Status == GameStatus.Active;

        var dto = new GetGameDto
        {
            Id = game.Id,
            Kind = game.Kind.ToString(),
            Status = game.Status.ToString(),
            Board = game.Board,
            Turn = game.Turn.ToString(),
            MyMark = myMark,
            IsMyTurn = active && myMark == game.Turn.ToString(),
            IsInvitedToMe = invited && game.InviteeId == viewerId,
            IsInvitedByMe = invited && game.InviterId == viewerId,
            PlayerX = game.PlayerXId.HasValue ? ToPlayer(game.PlayerXId.Value, people) : null,
            PlayerO = game.PlayerOId.HasValue ? ToPlayer(game.PlayerOId.Value, people) : null,
            Inviter = ToPlayer(game.InviterId, people),
            Invitee = ToPlayer(game.InviteeId, people),
            Opponent = ToPlayer(opponentId, people),
            WinnerId = game.WinnerId,
            Outcome = game.Outcome?.ToString(),
            WinningLine = game.Outcome == GameOutcome.Win ? TicTacToe.FindWinningLine(game.Board) : null,
            CreatedAt = game.CreatedAt,
            UpdatedAt = game.UpdatedAt,
            FinishedAt = game.FinishedAt
        };

        if (game.Kind == GameKind.RockPaperScissors)
            FillRockPaperScissors(dto, game, viewerId);
        else if (game.Kind == GameKind.Battleship)
            FillBattleship(dto, game, viewerId);

        return dto;
    }

    private static void FillBattleship(GetGameDto dto, Game game, Guid viewerId)
    {
        var viewerIsInviter = game.InviterId == viewerId;
        var myBoard = viewerIsInviter ? game.InviterBoard : game.InviteeBoard;
        var opponentBoard = viewerIsInviter ? game.InviteeBoard : game.InviterBoard;
        var myShots = viewerIsInviter ? game.InviterShots : game.InviteeShots;
        var opponentShots = viewerIsInviter ? game.InviteeShots : game.InviterShots;
        var active = game.Status == GameStatus.Active;
        var finished = game.Status == GameStatus.Finished;

        dto.IAmReady = myBoard.Length > 0;
        dto.OpponentReady = opponentBoard.Length > 0;
        var battle = dto.IAmReady && dto.OpponentReady;
        dto.Phase = active ? (battle ? "Battle" : "Placing") : null;
        dto.IsMyTurn = active && (battle ? game.Turn.ToString() == dto.MyMark : !dto.IAmReady);

        dto.MyShips = BattleshipGame.FindShips(myBoard)
            .Select(s => ToShipDto(s, BattleshipGame.IsSunk(myBoard, opponentShots, s.Kind)))
            .ToList();
        dto.OpponentShips = BattleshipGame.FindShips(opponentBoard)
            .Select(s => ToShipDto(s, BattleshipGame.IsSunk(opponentBoard, myShots, s.Kind)))
            .Where(s => s.Sunk || finished)
            .ToList();

        dto.MyShots = myShots.Length == BattleshipGame.Cells ? myShots : BattleshipGame.EmptyShots();
        dto.OpponentShots = opponentShots.Length == BattleshipGame.Cells ? opponentShots : BattleshipGame.EmptyShots();
        dto.MyShipsLeft = BattleshipGame.ShipsLeft(myBoard, opponentShots);
        dto.OpponentShipsLeft = BattleshipGame.ShipsLeft(opponentBoard, myShots);

        if (game.LastShotCell != null && game.LastShotResult != null)
            dto.LastShot = new GetGameShotDto { Cell = game.LastShotCell.Value, Result = game.LastShotResult.Value.ToString(), ByMe = game.LastShotById == viewerId };
    }

    private static GetGameShipDto ToShipDto(BattleshipGame.PlacedShip ship, bool sunk) => new()
    {
        Ship = ship.Kind.ToString(),
        Row = ship.Row,
        Col = ship.Col,
        Horizontal = ship.Horizontal,
        Length = ship.Length,
        Sunk = sunk
    };

    private static void FillRockPaperScissors(GetGameDto dto, Game game, Guid viewerId)
    {
        var viewerIsInviter = game.InviterId == viewerId;
        var myPick = viewerIsInviter ? game.InviterChoice : game.InviteeChoice;
        var opponentPick = viewerIsInviter ? game.InviteeChoice : game.InviterChoice;
        var active = game.Status == GameStatus.Active;

        dto.MyChoice = active ? myPick?.ToString() : null;
        dto.OpponentHasChosen = active && opponentPick != null;
        dto.IsMyTurn = active && myPick == null;

        var rounds = RockPaperScissors.ParseRounds(game.Rounds);
        dto.Rounds = rounds.Select(r =>
        {
            var (mine, theirs) = viewerIsInviter ? (r.Inviter, r.Invitee) : (r.Invitee, r.Inviter);
            var result = RockPaperScissors.Compare(mine, theirs);
            return new GetGameRoundDto
            {
                MyChoice = mine.ToString(),
                OpponentChoice = theirs.ToString(),
                Result = result > 0 ? "Win" : result < 0 ? "Loss" : "Draw"
            };
        }).ToList();

        var (inviterWins, inviteeWins, played) = RockPaperScissors.Score(game.Rounds);
        dto.MyScore = viewerIsInviter ? inviterWins : inviteeWins;
        dto.OpponentScore = viewerIsInviter ? inviteeWins : inviterWins;
        dto.RoundNumber = game.Status == GameStatus.Finished ? played : Math.Min(played + 1, RockPaperScissors.TotalRounds);
        dto.TotalRounds = RockPaperScissors.TotalRounds;
    }

    private static GetGamePlayerDto ToPlayer(Guid userId, Dictionary<Guid, UserSummary> people)
    {
        return people.TryGetValue(userId, out var person)
            ? new GetGamePlayerDto { Id = userId, Name = person.FullName, AvatarUrl = person.AvatarUrl }
            : new GetGamePlayerDto { Id = userId };
    }
}
