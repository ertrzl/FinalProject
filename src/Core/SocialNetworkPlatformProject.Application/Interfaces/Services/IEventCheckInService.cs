using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The attendance list ("Yoklama") of an event: the organizer ticks the people who really came.
public interface IEventCheckInService
{
    // Organizer only (ForbiddenException otherwise): everyone going, with their ticks and the counts. Always readable;
    // IsOpen says whether it can still be changed.
    Task<GetEventAttendanceDto> GetAsync(Guid currentUserId, Guid eventId);

    // Organizer only, from the start of the event until EventLimits.CheckInGrace after its end, and only for people who
    // are going. Returns the list as it is afterwards.
    Task<GetEventAttendanceDto> SetAsync(Guid currentUserId, Guid eventId, Guid userId, PutEventCheckInDto dto);
}
