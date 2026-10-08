using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IHashtagRepository : IRepository<Hashtag>
{
    // Connects a post to a hashtag (a new PostHashtag row). Both must already be tracked, the post may be new or existing.
    // Always add the link through here, never by putting a new PostHashtag into post.Hashtags: its Id is already filled
    // in, so for a post that is already in the database EF would take it for an existing row and try to UPDATE it.
    Task LinkAsync(Post post, Hashtag hashtag);
}
