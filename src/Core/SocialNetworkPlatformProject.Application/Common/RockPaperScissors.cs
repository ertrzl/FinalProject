using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

public static class RockPaperScissors
{
    public const int TotalRounds = 3;
    public const int WinsNeeded = 2;

    public static int Compare(RpsChoice inviter, RpsChoice invitee)
    {
        if (inviter == invitee) return 0;
        return Beats(inviter, invitee) ? 1 : -1;
    }

    private static bool Beats(RpsChoice a, RpsChoice b) =>
        (a == RpsChoice.Rock && b == RpsChoice.Scissors)
        || (a == RpsChoice.Scissors && b == RpsChoice.Paper)
        || (a == RpsChoice.Paper && b == RpsChoice.Rock);

    public static string AppendRound(string rounds, RpsChoice inviter, RpsChoice invitee) =>
        rounds + (int)inviter + (int)invitee;

    public static List<(RpsChoice Inviter, RpsChoice Invitee)> ParseRounds(string rounds)
    {
        var list = new List<(RpsChoice, RpsChoice)>();
        for (var i = 0; i + 1 < rounds.Length; i += 2)
            list.Add(((RpsChoice)(rounds[i] - '0'), (RpsChoice)(rounds[i + 1] - '0')));
        return list;
    }

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
