using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventCommentService
{
    // Announcements first, then comments newest first.
    Task<PagedResult<GetEventCommentDto>> GetAsync(Guid currentUserId, Guid eventId, int page, int pageSize);

    // Anyone who can see the event may comment; only the organizer may post an announcement (which notifies attendees).
    Task<GetEventCommentDto> CreateAsync(Guid currentUserId, Guid eventId, PostEventCommentDto dto);

    // The author or the event's organizer.
    Task DeleteAsync(Guid currentUserId, Guid eventId, Guid commentId);
}
