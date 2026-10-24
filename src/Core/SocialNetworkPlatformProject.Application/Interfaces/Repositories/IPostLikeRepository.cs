using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostLikeRepository : IRepository<PostLike>
{
    Task<Dictionary<Guid, Dictionary<ReactionType, int>>> GetReactionCountsAsync(IReadOnlyCollection<Guid> postIds);

    Task<Dictionary<Guid, ReactionType>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> postIds);

    Task<List<PostLike>> GetNewestAsync(Guid postId, ReactionType? type, int take);
}
