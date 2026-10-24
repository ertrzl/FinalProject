using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IHashtagRepository : IRepository<Hashtag>
{
    Task LinkAsync(Post post, Hashtag hashtag);

    Task<int> DeleteUnusedCreatedBeforeAsync(DateTime limitUtc);
}
