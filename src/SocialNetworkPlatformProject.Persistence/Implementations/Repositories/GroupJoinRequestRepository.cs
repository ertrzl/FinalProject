using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GroupJoinRequestRepository : Repository<GroupJoinRequest>, IGroupJoinRequestRepository
{
    public GroupJoinRequestRepository(ApplicationDbContext context) : base(context)
    {
    }
}
