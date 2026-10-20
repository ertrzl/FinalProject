using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupPinnedPostRepository : IRepository<GroupPinnedPost>
{
    // The ids of the posts pinned on a group's wall, the most recently pinned first.
    Task<List<Guid>> GetPinnedPostIdsAsync(Guid groupId);
}
