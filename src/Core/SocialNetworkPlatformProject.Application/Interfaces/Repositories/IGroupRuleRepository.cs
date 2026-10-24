using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGroupRuleRepository : IRepository<GroupRule>
{
    Task<List<GroupRule>> GetForGroupAsync(Guid groupId);

    Task<Dictionary<Guid, int>> CountByGroupAsync(IReadOnlyCollection<Guid> groupIds);
}
