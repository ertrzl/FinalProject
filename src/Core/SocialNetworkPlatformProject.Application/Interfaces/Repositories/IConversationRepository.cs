using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    // The direct conversation of these two people, if they have one.
    Task<Guid?> FindDirectAsync(Guid userA, Guid userB);

    // The direct conversation of these two people, created (and saved) if they have none yet. When two requests
    // try to create it at the same moment the unique DirectKey lets one win and the other uses the winner's.
    Task<Guid> GetOrCreateDirectAsync(Guid userA, Guid userB);

    // The chat of a group, if it has one yet.
    Task<Guid?> FindGroupAsync(Guid groupId);

    // The chat of a group, created (and saved) with every current member as a participant if it has none yet. As with
    // direct conversations, the unique GroupId settles two first openings at the same moment.
    Task<Guid> GetOrCreateGroupAsync(Guid groupId);

    // Puts the user's group chats in step with their memberships: a chat for every group they are in (created when
    // missing), them a participant of each, and no longer a participant of the chat of a group they left. GroupService
    // keeps this up to date as people join and leave; this repairs what an interrupted request may have left behind.
    Task SyncGroupChatsAsync(Guid userId);

    // conversation id -> group id, for those of these conversations that are group chats.
    Task<Dictionary<Guid, Guid>> GetGroupIdsAsync(IReadOnlyCollection<Guid> conversationIds);

    // The photos sent in a group's chat (deleted with the group, so their files must go too).
    Task<List<string>> GetGroupChatMediaUrlsAsync(Guid groupId);
}
