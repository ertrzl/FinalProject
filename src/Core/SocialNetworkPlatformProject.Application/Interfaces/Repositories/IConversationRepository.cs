using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    // Entity-specific query methods will be added here as the Conversation service needs them.
}
