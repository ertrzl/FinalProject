namespace SocialNetworkPlatformProject.Application.Common;

// The rules of the board itself: where three in a row can be and what a full board means. Whose turn it is and who
// may play stay in GameService.
public static class TicTacToe
{
    public const char Empty = '-';

    // Rows, columns, diagonals: the cells (0 = top left, 8 = bottom right) that win together.
    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
    };

    // The cells of the first line with the same mark in all three, or null when nobody has one.
    public static int[]? FindWinningLine(string board)
    {
        return Lines.FirstOrDefault(line => board[line[0]] != Empty && board[line[0]] == board[line[1]] && board[line[1]] == board[line[2]]);
    }

    public static bool IsFull(string board) => !board.Contains(Empty);
}
