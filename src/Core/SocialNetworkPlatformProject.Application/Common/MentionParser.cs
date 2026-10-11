using System.Text.RegularExpressions;
using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.Common;

// Finds the "@username" tags in a post or comment text. Pure text work: whether the names belong to real people is
// decided elsewhere (IMentionService).
public static class MentionParser
{
    // A user name is letters, digits, dots and underscores (see the register rules). The "@" must not be glued to a
    // word before it, so an e-mail address such as ali@mail.com is not a tag.
    private static readonly Regex Pattern = new(@"(?<![\p{L}0-9_.@])@([a-zA-Z0-9._]{1,50})", RegexOptions.Compiled);

    // The distinct user names tagged in the text, lower-case, in the order they appear. A dot that ends the tag is
    // sentence punctuation ("thanks @ali."), not part of the name. At most PostLimits.MaxMentions are returned.
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

    // The tagged people of one text, out of everybody that was looked up (keyed by lower-case user name).
    public static List<GetMentionDto> Pick(string? text, IReadOnlyDictionary<string, GetMentionDto> resolved)
    {
        return ExtractUserNames(text)
            .Where(resolved.ContainsKey)
            .Select(name => resolved[name])
            .ToList();
    }
}
