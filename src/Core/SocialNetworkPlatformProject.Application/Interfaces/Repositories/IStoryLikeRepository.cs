using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryLikeRepository : IRepository<StoryLike>
{
    Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds);

    Task<HashSet<Guid>> GetLikedStoryIdsAsync(Guid userId, IReadOnlyCollection<Guid> storyIds);

    Task<HashSet<Guid>> GetLikerIdsAsync(Guid storyId);
}
