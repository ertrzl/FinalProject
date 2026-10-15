using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    // Removes the notifications older than the given time, read or not; returns how many went.
    Task<int> DeleteOlderThanAsync(DateTime limitUtc);
}
