using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// "@username" tags in posts and comments. Nothing is stored: the tags are read from the text each time, so editing a
// text can never leave a stale tag behind.
public interface IMentionService
{
    // The real people tagged in any of these texts, keyed by lower-case user name (one query for all of them).
    // Pick the ones of a single text with MentionParser.Pick.
    Task<Dictionary<string, GetMentionDto>> ResolveAsync(IEnumerable<string?> texts);

    // Tells the people newly tagged in `text` (those in it but not in previousText, so an edit never repeats an old
    // tag). Not the author, not people who were just told about the same action in another way (alreadyNotified:
    // a comment already notifies the post's author), and not anybody who may not see the post: a tag must not leak a
    // hidden post.
    Task NotifyAsync(Post post, Guid actorId, string? text, string? previousText, Guid? commentId = null,
        IReadOnlyCollection<Guid>? alreadyNotified = null);
}
