using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// The rules of rock-paper-scissors: what beats what, how the scored rounds are written down, and when a match is over.
// A match is best of three: it ends when somebody has two round wins, or after three rounds. Whose pick is still
// secret and who may play stay in GameService.
public static class RockPaperScissors
{
    public const int TotalRounds = 3;
    public const int WinsNeeded = 2;

    // 1 when the inviter's pick wins the round, -1 when the invitee's does, 0 when both put out the same.
    public static int Compare(RpsChoice inviter, RpsChoice invitee)
    {
        if (inviter == invitee) return 0;
        return Beats(inviter, invitee) ? 1 : -1;
    }

    private static bool Beats(RpsChoice a, RpsChoice b) =>
        (a == RpsChoice.Rock && b == RpsChoice.Scissors)
        || (a == RpsChoice.Scissors && b == RpsChoice.Paper)
        || (a == RpsChoice.Paper && b == RpsChoice.Rock);

    // The scored rounds are stored as two digits each (the inviter's pick, then the invitee's), e.g. "0212" = two rounds.
    public static string AppendRound(string rounds, RpsChoice inviter, RpsChoice invitee) =>
        rounds + (int)inviter + (int)invitee;

    public static List<(RpsChoice Inviter, RpsChoice Invitee)> ParseRounds(string rounds)
    {
        var list = new List<(RpsChoice, RpsChoice)>();
        for (var i = 0; i + 1 < rounds.Length; i += 2)
            list.Add(((RpsChoice)(rounds[i] - '0'), (RpsChoice)(rounds[i + 1] - '0')));
        return list;
    }

    // Round wins of each side, and how many rounds were played.
    public static (int InviterWins, int InviteeWins, int Played) Score(string rounds)
    {
        int inviterWins = 0, inviteeWins = 0;
        var parsed = ParseRounds(rounds);
        foreach (var (inviter, invitee) in parsed)
        {
            var result = Compare(inviter, invitee);
            if (result > 0) inviterWins++;
            else if (result < 0) inviteeWins++;
        }
        return (inviterWins, inviteeWins, parsed.Count);
    }

    public static bool IsOver(int inviterWins, int inviteeWins, int played) =>
        inviterWins >= WinsNeeded || inviteeWins >= WinsNeeded || played >= TotalRounds;
}
