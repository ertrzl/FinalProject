using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostMentionRepository : IRepository<PostMention>
{
    IQueryable<Guid> PostIdsMentioning(Guid userId);
}
