using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupPinnedPostRepository : IRepository<GroupPinnedPost>
{
    Task<List<Guid>> GetPinnedPostIdsAsync(Guid groupId);
}
