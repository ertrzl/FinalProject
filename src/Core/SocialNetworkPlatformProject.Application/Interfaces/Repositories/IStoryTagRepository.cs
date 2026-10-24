using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryTagRepository : IRepository<StoryTag>
{
    Task<Dictionary<Guid, List<Guid>>> GetTaggedUserIdsAsync(IReadOnlyCollection<Guid> storyIds);

    Task<HashSet<Guid>> GetStoryIdsTaggingAsync(Guid userId, IReadOnlyCollection<Guid> storyIds);
}
