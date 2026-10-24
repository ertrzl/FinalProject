using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventCheckInService
{
    Task<GetEventAttendanceDto> GetAsync(Guid currentUserId, Guid eventId);

    Task<GetEventAttendanceDto> SetAsync(Guid currentUserId, Guid eventId, Guid userId, PutEventCheckInDto dto);
}
