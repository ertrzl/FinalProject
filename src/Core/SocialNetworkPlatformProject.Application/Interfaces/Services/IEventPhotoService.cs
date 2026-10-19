using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The photo album of an event ("Fotoğraflar" tab). Everything goes through IEventAccessService first, so a private
// or group event's pictures are only for the people who may see that event.
public interface IEventPhotoService
{
    // Anyone who can see the event.
    Task<GetEventPhotosDto> GetAsync(Guid currentUserId, Guid eventId);

    // The organizer and the people who are going, at most EventLimits.MaxPhotosPerPerson each and
    // EventLimits.MaxPhotosPerEvent in all. Images only. Returns the whole album as the viewer sees it afterwards.
    Task<GetEventPhotosDto> AddAsync(Guid currentUserId, Guid eventId, PostEventPhotosDto dto);

    // The person who added the picture, or the organizer.
    Task DeleteAsync(Guid currentUserId, Guid eventId, Guid photoId);
}
