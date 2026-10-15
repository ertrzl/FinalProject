using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ICommentService
{
    // A page of the post's top-level comments (oldest first), each followed by its replies; the page size counts the
    // top-level comments, so a thread is never cut in two.
    Task<GetCommentPageDto> GetByPostAsync(Guid currentUserId, Guid postId, int page, int pageSize);

    Task<GetCommentDto> AddAsync(Guid currentUserId, PostCommentDto dto);

    // asAdmin: the site's admin removing it (only the admin panel passes true).
    Task DeleteAsync(Guid currentUserId, Guid commentId, bool asAdmin = false);

    Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid commentId);
}
