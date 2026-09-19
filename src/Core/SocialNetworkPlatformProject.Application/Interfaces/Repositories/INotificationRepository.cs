using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    // Entity-specific query methods will be added here as the Notification service needs them.
}
