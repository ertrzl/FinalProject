using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The stored copy of the "@username" tags of posts (PostMention) and the profile's "Etiketlendiklerim" tab built on it.
// IMentionService reads tags from the text for display and notifications; this keeps them in a table so they can be searched.
public interface IPostMentionService
{
    // Makes the post's stored tags match `text`: tags no longer in the text are removed, new ones added. Not the author, and only
    // people who exist. Call it after the post is saved (create, edit, share).
    Task SyncAsync(Post post, string? text);

    // The posts this person is tagged in, newest first, for the viewer: only posts the viewer may see (privacy, groups, blocks) and
    // the tagged person may see too. A private account's tagged posts are only for the person and their friends. Somebody who blocked
    // the viewer looks as if they do not exist.
    Task<PagedResult<GetPostDto>> GetTaggedPostsAsync(Guid profileUserId, Guid viewerId, int page, int pageSize);

    // Fills the table for posts written before it existed. Safe to call again; does nothing once there are rows. Returns how many tags it stored.
    Task<int> BackfillAsync();
}
