using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventPhotoRepository : IRepository<EventPhoto>
{
    // The album of one event, newest first (an event holds at most EventLimits.MaxPhotosPerEvent).
    Task<List<EventPhoto>> GetByEventAsync(Guid eventId);

    Task<int> CountByEventAsync(Guid eventId);

    // How many of the event's pictures this person added.
    Task<int> CountByUploaderAsync(Guid eventId, Guid userId);

    // The event whose album holds the picture with this address; null when there is none (UploadsController asks).
    Task<Guid?> FindEventIdByUrlAsync(string url);

    // The addresses of every picture of an event: read before the event is deleted, so the files can be removed after.
    Task<List<string>> GetUrlsByEventAsync(Guid eventId);
}
