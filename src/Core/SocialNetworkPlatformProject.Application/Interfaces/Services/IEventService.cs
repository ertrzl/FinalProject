using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventService
{
    Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto);

    Task<GetEventDto> UpdateAsync(Guid currentUserId, Guid eventId, PutEventDto dto);

    Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId, bool asAdmin = false);

    Task RemoveUserFromGroupEventsAsync(Guid userId, Guid groupId, Guid groupOwnerId);
}
