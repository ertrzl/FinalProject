using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message>
{
    // Entity-specific query methods will be added here as the Message service needs them.
}
