using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventCommentService
{
    Task<PagedResult<GetEventCommentDto>> GetAsync(Guid currentUserId, Guid eventId, int page, int pageSize);

    Task<GetEventCommentDto> CreateAsync(Guid currentUserId, Guid eventId, PostEventCommentDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId, Guid commentId);
}
