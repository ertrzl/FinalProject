using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryViewRepository : IRepository<StoryView>
{
    // How many people watched each of these stories (stories nobody watched are left out).
    Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds);

    // Which of these stories the viewer has already watched.
    Task<HashSet<Guid>> GetViewedStoryIdsAsync(Guid viewerId, IReadOnlyCollection<Guid> storyIds);

    // The latest viewers of one story, newest first, at most `take` of them.
    Task<List<StoryView>> GetNewestAsync(Guid storyId, int take);
}
