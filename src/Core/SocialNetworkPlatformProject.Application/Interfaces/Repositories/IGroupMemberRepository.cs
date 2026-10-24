using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupMemberRepository : IRepository<GroupMember>
{
    Task<(List<GroupMember> Items, int TotalCount)> SearchAsync(
        Guid groupId, Guid ownerId, string? term, GroupMemberRole? role, bool ownerOnly, int page, int pageSize);
}
