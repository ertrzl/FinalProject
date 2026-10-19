using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// The rules of the board in battleship: the fleet, where ships may lie, and what a set of shots has done to a board.
// Whose turn it is and who may play stay in GameService.
//
// A board is a string of 100 characters, row by row (cell = row * 10 + column): '.' is water, a letter is a part of the
// ship with that letter. The shots one player fired are a second string of 100: '-' not shot, 'o' miss, 'x' hit.
public static class BattleshipGame
{
    public const int Size = 10;
    public const int Cells = Size * Size;
    public const char Water = '.';
    public const char NotShot = '-';
    public const char Miss = 'o';
    public const char Hit = 'x';

    public record ShipDef(ShipKind Kind, char Letter, int Length);
    public record Placement(ShipKind Kind, int Row, int Col, bool Horizontal);
    public record PlacedShip(ShipKind Kind, int Row, int Col, bool Horizontal, int Length);

    public static readonly IReadOnlyList<ShipDef> Fleet = new[]
    {
        new ShipDef(ShipKind.Carrier, 'C', 5),
        new ShipDef(ShipKind.Battleship, 'B', 4),
        new ShipDef(ShipKind.Cruiser, 'R', 3),
        new ShipDef(ShipKind.Submarine, 'S', 3),
        new ShipDef(ShipKind.Destroyer, 'D', 2)
    };

    public static string EmptyShots() => new string(NotShot, Cells);

    // One of each ship, all inside the board, none on another. Ships may touch.
    public static bool TryBuildBoard(IReadOnlyCollection<Placement> placements, out string board)
    {
        board = string.Empty;
        if (placements.Count != Fleet.Count || placements.Select(p => p.Kind).Distinct().Count() != Fleet.Count)
            return false;

        var cells = new string(Water, Cells).ToCharArray();
        foreach (var placement in placements)
        {
            var def = Fleet.First(f => f.Kind == placement.Kind);
            for (var i = 0; i < def.Length; i++)
            {
                var row = placement.Row + (placement.Horizontal ? 0 : i);
                var col = placement.Col + (placement.Horizontal ? i : 0);
                if (row < 0 || row >= Size || col < 0 || col >= Size)
                    return false;

                var index = row * Size + col;
                if (cells[index] != Water)
                    return false;

                cells[index] = def.Letter;
            }
        }

        board = new string(cells);
        return true;
    }

    // The ships lying on a board, read back from its letters (an empty board: none).
    public static List<PlacedShip> FindShips(string board)
    {
        var ships = new List<PlacedShip>();
        if (board.Length != Cells)
            return ships;

        foreach (var def in Fleet)
        {
            var cells = Enumerable.Range(0, Cells).Where(i => board[i] == def.Letter).ToList();
            if (cells.Count == 0)
                continue;

            var first = cells[0];
            var horizontal = cells.Count == 1 || cells[1] == first + 1;
            ships.Add(new PlacedShip(def.Kind, first / Size, first % Size, horizontal, def.Length));
        }

        return ships;
    }

    // Every cell of this ship has been hit (no shots at all: nothing is sunk).
    public static bool IsSunk(string board, string shots, char letter)
    {
        if (board.Length != Cells || shots.Length != Cells)
            return false;

        var any = false;
        for (var i = 0; i < Cells; i++)
        {
            if (board[i] != letter) continue;
            any = true;
            if (shots[i] != Hit) return false;
        }

        return any;
    }

    public static bool IsSunk(string board, string shots, ShipKind kind) =>
        IsSunk(board, shots, Fleet.First(f => f.Kind == kind).Letter);

    // Every ship of the board is sunk.
    public static bool AllSunk(string board, string shots) =>
        board.Length == Cells && Fleet.All(f => IsSunk(board, shots, f.Letter));

    public static int ShipsLeft(string board, string shots) =>
        board.Length != Cells ? Fleet.Count : Fleet.Count(f => !IsSunk(board, shots, f.Letter));

    // A random valid layout (the "Rastgele diz" button).
    public static List<Placement> RandomFleet(Random random)
    {
        while (true)
        {
            var placements = new List<Placement>();
            var cells = new string(Water, Cells).ToCharArray();
            var failed = false;

            foreach (var def in Fleet)
            {
                var placed = false;
                for (var attempt = 0; attempt < 200 && !placed; attempt++)
                {
                    var horizontal = random.Next(2) == 0;
                    var row = random.Next(horizontal ? Size : Size - def.Length + 1);
                    var col = random.Next(horizontal ? Size - def.Length + 1 : Size);

                    var free = true;
                    for (var i = 0; i < def.Length && free; i++)
                        free = cells[(row + (horizontal ? 0 : i)) * Size + col + (horizontal ? i : 0)] == Water;
                    if (!free) continue;

                    for (var i = 0; i < def.Length; i++)
                        cells[(row + (horizontal ? 0 : i)) * Size + col + (horizontal ? i : 0)] = def.Letter;

                    placements.Add(new Placement(def.Kind, row, col, horizontal));
                    placed = true;
                }

                if (!placed) { failed = true; break; }
            }

            if (!failed)
                return placements;
        }
    }
}
