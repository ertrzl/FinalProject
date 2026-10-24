using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventPhotoRepository : IRepository<EventPhoto>
{
    Task<List<EventPhoto>> GetByEventAsync(Guid eventId);

    Task<int> CountByEventAsync(Guid eventId);

    Task<int> CountByUploaderAsync(Guid eventId, Guid userId);

    Task<Guid?> FindEventIdByUrlAsync(string url);

    Task<List<string>> GetUrlsByEventAsync(Guid eventId);
}
