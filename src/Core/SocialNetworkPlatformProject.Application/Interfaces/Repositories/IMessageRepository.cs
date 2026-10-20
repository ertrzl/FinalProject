using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message>
{
    // Group chats: how many messages the user has not read in each of these chats, i.e. the others' messages (not taken
    // back, not from someone in hiddenSenderIds) sent after the user's ConversationParticipant.LastReadAt (or after they
    // joined). Chats with nothing unread are left out.
    Task<Dictionary<Guid, int>> CountGroupUnreadAsync(Guid userId, IReadOnlyCollection<Guid> conversationIds, IReadOnlyCollection<Guid> hiddenSenderIds);
}
