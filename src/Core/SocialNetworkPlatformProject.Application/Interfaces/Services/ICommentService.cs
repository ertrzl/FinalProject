using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ICommentService
{
    Task<GetCommentPageDto> GetByPostAsync(Guid currentUserId, Guid postId, int page, int pageSize);

    Task<GetCommentDto> AddAsync(Guid currentUserId, PostCommentDto dto);

    Task DeleteAsync(Guid currentUserId, Guid commentId, bool asAdmin = false);

    Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid commentId);
}
