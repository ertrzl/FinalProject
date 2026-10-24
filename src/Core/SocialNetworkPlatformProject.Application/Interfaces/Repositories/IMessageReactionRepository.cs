using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMessageReactionRepository : IRepository<MessageReaction>
{
    Task<List<MessageReaction>> GetForMessagesAsync(IReadOnlyCollection<Guid> messageIds);

    Task DeleteForMessageAsync(Guid messageId);
}
