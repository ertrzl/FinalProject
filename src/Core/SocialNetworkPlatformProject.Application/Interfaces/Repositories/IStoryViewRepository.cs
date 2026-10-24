using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryViewRepository : IRepository<StoryView>
{
    Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds);

    Task<HashSet<Guid>> GetViewedStoryIdsAsync(Guid viewerId, IReadOnlyCollection<Guid> storyIds);

    Task<List<StoryView>> GetNewestAsync(Guid storyId, int take);
}
