using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface ICommentRepository : IRepository<Comment>
{
    // Entity-specific query methods will be added here as the Comment service needs them.
}
