using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventService
{
    Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto);

    Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId);

    // Events that haven't started yet, soonest first.
    Task<List<GetEventDto>> GetUpcomingAsync(Guid currentUserId);

    Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId);
}
