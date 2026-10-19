using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostMentionRepository : IRepository<PostMention>
{
    // The ids of the posts this person is tagged in, as a query part (it is meant to be used inside a bigger post query).
    IQueryable<Guid> PostIdsMentioning(Guid userId);
}
