using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SocialNetworkPlatformProject.Application.Common;

// Every text the API shows a person ("Post not found.", validation messages, ...) lives in a language file:
// Locales/<code>.json (tr.json, en.json, ...), each a flat { "key": "text" } list that is built into this assembly.
// The code only names the key:
//   throw new NotFoundException(Loc.T("post.notFound"));
//   throw new TooManyRequestsException(Loc.T("auth.tooManyAttempts", minutes));   // "... {0} minute(s)"
//
// ADDING A LANGUAGE = adding Locales/<code>.json with the same keys (and wwwroot/locales/<code>.js + one line in
// wwwroot/locales/languages.js for the pages). No code changes: the languages are found from the files.
//
// The language is the one of the request (the page sends it as Accept-Language, see Program.cs), found in the
// current UI culture. A key missing in a language falls back to the default language, then to the key itself.
public static class Loc
{
    public const string DefaultLanguage = "tr";

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Texts = new(Load);

    // Every language there is a file for ("tr", "en", ...).
    public static IReadOnlyCollection<string> Languages => Texts.Value.Keys;

    // The language of the current request, always one of Languages.
    public static string CurrentLanguage
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Texts.Value.ContainsKey(code) ? code : DefaultLanguage;
        }
    }

    // The text for the key in the request's language; {0}, {1}... are filled with the arguments.
    public static string T(string key, params object[] args)
    {
        var text = Find(CurrentLanguage, key) ?? Find(DefaultLanguage, key) ?? key;
        return args.Length == 0 ? text : string.Format(CultureInfo.CurrentCulture, text, args);
    }

    // For tests and checks: the keys of one language file.
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
            // "SocialNetworkPlatformProject.Application.Locales.tr.json" -> "tr"
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
