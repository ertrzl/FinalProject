using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryHighlightItemRepository : IRepository<StoryHighlightItem>
{
    Task<Guid?> FindOwnerByMediaUrlAsync(string url);
}
