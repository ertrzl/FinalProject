using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Guid?> FindDirectAsync(Guid userA, Guid userB);

    Task<Guid> GetOrCreateDirectAsync(Guid userA, Guid userB);

    Task<Guid?> FindGroupAsync(Guid groupId);

    Task<Guid> GetOrCreateGroupAsync(Guid groupId);

    Task SyncGroupChatsAsync(Guid userId);

    Task<Dictionary<Guid, Guid>> GetGroupIdsAsync(IReadOnlyCollection<Guid> conversationIds);

    Task<HashSet<Guid>> GetGroupChatIdsAsync(IReadOnlyCollection<Guid> conversationIds);

    Task<List<string>> GetMediaUrlsAsync(Guid conversationId);

    Task<List<string>> GetGroupChatMediaUrlsAsync(Guid groupId);
}
