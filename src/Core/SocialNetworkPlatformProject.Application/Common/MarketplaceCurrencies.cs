using System.Globalization;

namespace SocialNetworkPlatformProject.Application.Common;

public static class MarketplaceCurrencies
{
    public const string Default = "TRY";

    private static readonly Dictionary<string, string> Symbols = new()
    {
        ["TRY"] = "₺",
        ["USD"] = "$",
        ["EUR"] = "€",
        ["AZN"] = "₼",
        ["GBP"] = "£"
    };

    public static readonly IReadOnlyList<string> All = Symbols.Keys.ToList();

    public static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? Default : code.Trim().ToUpperInvariant();

    public static bool IsValid(string? code) => All.Contains(Normalize(code));

    public static string Format(decimal amount, string? code)
    {
        var normalized = Normalize(code);
        var symbol = Symbols.TryGetValue(normalized, out var found) ? found : normalized;
        return amount.ToString("0.##", CultureInfo.InvariantCulture) + " " + symbol;
    }
}
