using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupRuleRepository : IRepository<GroupRule>
{
    // A group's rules in their order.
    Task<List<GroupRule>> GetForGroupAsync(Guid groupId);

    // How many rules each of these groups has (groups without rules are left out).
    Task<Dictionary<Guid, int>> CountByGroupAsync(IReadOnlyCollection<Guid> groupIds);
}
