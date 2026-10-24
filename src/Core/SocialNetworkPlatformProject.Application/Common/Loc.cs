using System.Globalization;
using System.Text.Json;

namespace SocialNetworkPlatformProject.Application.Common;

public static class Loc
{
    public const string DefaultLanguage = "tr";

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Texts = new(Load);

    public static IReadOnlyCollection<string> Languages => Texts.Value.Keys;

    public static string CurrentLanguage
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Texts.Value.ContainsKey(code) ? code : DefaultLanguage;
        }
    }

    public static string T(string key, params object[] args)
    {
        var text = Find(CurrentLanguage, key) ?? Find(DefaultLanguage, key) ?? key;
        return args.Length == 0 ? text : string.Format(CultureInfo.CurrentCulture, text, args);
    }

    public static IReadOnlyCollection<string> Keys(string language) =>
        Texts.Value.TryGetValue(language, out var texts) ? texts.Keys : Array.Empty<string>();

    private static string? Find(string language, string key) =>
        Texts.Value.TryGetValue(language, out var texts) && texts.TryGetValue(key, out var text) ? text : null;

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var assembly = typeof(Loc).Assembly;
        var all = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            const string marker = ".Locales.";
            var at = resource.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0 || !resource.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            var code = resource[(at + marker.Length)..^".json".Length];
            using var stream = assembly.GetManifestResourceStream(resource)!;
            all[code] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
        }

        return all;
    }
}
