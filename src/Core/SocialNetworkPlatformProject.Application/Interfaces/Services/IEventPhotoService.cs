using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventPhotoService
{
    Task<GetEventPhotosDto> GetAsync(Guid currentUserId, Guid eventId);

    Task<GetEventPhotosDto> AddAsync(Guid currentUserId, Guid eventId, PostEventPhotosDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId, Guid photoId);
}
