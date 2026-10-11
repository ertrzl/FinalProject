using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostLikeRepository : IRepository<PostLike>
{
    // How many people answered each of these posts with each reaction (posts nobody reacted to are left out).
    Task<Dictionary<Guid, Dictionary<ReactionType, int>>> GetReactionCountsAsync(IReadOnlyCollection<Guid> postIds);

    // The viewer's own reaction on each of these posts (posts they did not react to are left out).
    Task<Dictionary<Guid, ReactionType>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> postIds);

    // The latest reactions on one post, optionally of one type only, at most `take` of them.
    Task<List<PostLike>> GetNewestAsync(Guid postId, ReactionType? type, int take);
}
