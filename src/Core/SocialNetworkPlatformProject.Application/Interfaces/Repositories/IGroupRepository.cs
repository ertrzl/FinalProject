using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupRepository : IRepository<Group>
{
    Task<Dictionary<Guid, GroupMembership>> GetMembershipsAsync(IReadOnlyCollection<Guid> groupIds, Guid viewerId);

    Task<bool> IsActivityMutedAsync(Guid userId, Guid? postId, Guid? eventId);
}
