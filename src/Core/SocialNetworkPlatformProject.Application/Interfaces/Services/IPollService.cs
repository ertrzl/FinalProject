using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPollService
{
    Task<GetPostDto> VoteAsync(Guid currentUserId, Guid postId, Guid optionId);
}
