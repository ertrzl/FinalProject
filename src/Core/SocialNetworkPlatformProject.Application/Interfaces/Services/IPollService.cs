using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPollService
{
    // The user votes in the poll of a post they may see. One vote per person, final; not after the poll ended;
    // the answer must belong to that poll. Returns the post as the voter sees it now (with the results).
    Task<GetPostDto> VoteAsync(Guid currentUserId, Guid postId, Guid optionId);
}
