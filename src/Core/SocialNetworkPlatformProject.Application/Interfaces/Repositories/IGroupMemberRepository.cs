using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupMemberRepository : IRepository<GroupMember>
{
    // Entity-specific query methods will be added here as the GroupMember service needs them.
}
