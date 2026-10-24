using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventQueryService
{
    Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId);

    Task<List<GetEventAttendeeDto>> GetAttendeesAsync(Guid currentUserId, Guid eventId, string? status);

    Task<PagedResult<GetEventDto>> GetUpcomingAsync(Guid currentUserId, EventQuery query);

    Task<List<GetEventDto>> GetGroupEventsAsync(Guid currentUserId, Guid groupId);

    Task<List<GetEventDto>> GetMineAsync(Guid currentUserId, string scope);
}
