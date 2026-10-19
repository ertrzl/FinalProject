using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryTagRepository : IRepository<StoryTag>
{
    // Who is tagged in each of these stories (stories nobody is tagged in are left out), in the order they were tagged.
    Task<Dictionary<Guid, List<Guid>>> GetTaggedUserIdsAsync(IReadOnlyCollection<Guid> storyIds);

    // Which of these stories the user is tagged in.
    Task<HashSet<Guid>> GetStoryIdsTaggingAsync(Guid userId, IReadOnlyCollection<Guid> storyIds);
}
