using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryLikeRepository : IRepository<StoryLike>
{
    // How many people liked each of these stories (stories nobody liked are left out).
    Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds);

    // Which of these stories the user has liked.
    Task<HashSet<Guid>> GetLikedStoryIdsAsync(Guid userId, IReadOnlyCollection<Guid> storyIds);

    // Everybody who liked one story.
    Task<HashSet<Guid>> GetLikerIdsAsync(Guid storyId);
}
