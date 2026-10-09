using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Turns posts into what a page shows, for one viewer: author, counts, reactions, the viewer's own reaction, saved,
// may-edit/delete, tagged people, the shared original and the poll. In batches (a fixed number of queries for a whole
// page). The posts must have been loaded with their Hashtags.Hashtag, Media, Poll.Options.
public interface IPostDtoBuilder
{
    Task<List<GetPostDto>> BuildAsync(IEnumerable<Post> posts, Guid viewerId);
}
