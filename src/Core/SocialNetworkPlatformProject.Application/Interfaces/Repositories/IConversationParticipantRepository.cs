using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IConversationParticipantRepository : IRepository<ConversationParticipant>
{
    // Entity-specific query methods will be added here as the ConversationParticipant service needs them.
}
