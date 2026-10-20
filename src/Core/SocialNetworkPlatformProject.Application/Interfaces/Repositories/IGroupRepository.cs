using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupRepository : IRepository<Group>
{
    // For several groups at once, in one query: the member count and the viewer's role (the lists of groups show these without
    // needing the members themselves). Every given group is in the result.
    Task<Dictionary<Guid, GroupMembership>> GetMembershipsAsync(IReadOnlyCollection<Guid> groupIds, Guid viewerId);

    // Has the user muted the group a notification comes from? The group is the one of the post (a wall post, its
    // comments, likes, mentions, a pinned announcement) or of the event (a group event). False when neither belongs to a group.
    Task<bool> IsActivityMutedAsync(Guid userId, Guid? postId, Guid? eventId);
}
