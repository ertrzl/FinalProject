using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    // The direct conversation of these two people, if they have one.
    Task<Guid?> FindDirectAsync(Guid userA, Guid userB);

    // The direct conversation of these two people, created (and saved) if they have none yet. When two requests
    // try to create it at the same moment the unique DirectKey lets one win and the other uses the winner's.
    Task<Guid> GetOrCreateDirectAsync(Guid userA, Guid userB);
}
