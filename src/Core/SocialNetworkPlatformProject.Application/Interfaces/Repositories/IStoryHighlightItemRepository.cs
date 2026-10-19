using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryHighlightItemRepository : IRepository<StoryHighlightItem>
{
    // The owner of the highlight that holds the file with this address; null when no highlight holds it.
    Task<Guid?> FindOwnerByMediaUrlAsync(string url);
}
