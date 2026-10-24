using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<int> DeleteOlderThanAsync(DateTime limitUtc);
}
