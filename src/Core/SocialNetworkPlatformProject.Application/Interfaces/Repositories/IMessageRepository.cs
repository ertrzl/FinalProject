using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message>
{
    Task<Dictionary<Guid, int>> CountGroupUnreadAsync(Guid userId, IReadOnlyCollection<Guid> conversationIds, IReadOnlyCollection<Guid> hiddenSenderIds);
}
