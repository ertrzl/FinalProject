using System.Text.RegularExpressions;
using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.Common;

public static class MentionParser
{
    private static readonly Regex Pattern = new(@"(?<![\p{L}0-9_.@])@([a-zA-Z0-9._]{1,50})", RegexOptions.Compiled);

    public static List<string> ExtractUserNames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return Pattern.Matches(text)
            .Select(match => match.Groups[1].Value.TrimEnd('.').ToLowerInvariant())
            .Where(name => name.Length > 0)
            .Distinct()
            .Take(PostLimits.MaxMentions)
            .ToList();
    }

    public static List<GetMentionDto> Pick(string? text, IReadOnlyDictionary<string, GetMentionDto> resolved)
    {
        return ExtractUserNames(text)
            .Where(resolved.ContainsKey)
            .Select(name => resolved[name])
            .ToList();
    }
}
