using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupMemberRepository : IRepository<GroupMember>
{
    // group.html "Üyeler": one page of a group's members whose name or user name contains the term (any member when it is
    // empty), only the owner (ownerOnly) or only one role (role). The owner first, then admins, moderators and members,
    // each by name. Counted and paged by the database.
    Task<(List<GroupMember> Items, int TotalCount)> SearchAsync(
        Guid groupId, Guid ownerId, string? term, GroupMemberRole? role, bool ownerOnly, int page, int pageSize);
}
