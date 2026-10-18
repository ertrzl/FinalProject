using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMessageReactionRepository : IRepository<MessageReaction>
{
    // Every reaction left on these messages.
    Task<List<MessageReaction>> GetForMessagesAsync(IReadOnlyCollection<Guid> messageIds);

    // Takes all the reactions off one message (it was taken back).
    Task DeleteForMessageAsync(Guid messageId);
}
