using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ICommentService
{
    // Flat list, oldest first; the frontend threads replies using ParentCommentId.
    Task<List<GetCommentDto>> GetByPostAsync(Guid currentUserId, Guid postId);

    Task<GetCommentDto> AddAsync(Guid currentUserId, PostCommentDto dto);

    Task DeleteAsync(Guid currentUserId, Guid commentId);

    Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid commentId);
}
