using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Group chats made on the Messages page ("Yeni grup"): friends picked by one person, a name of their own and optionally a
// picture. Sending and reading messages goes through IMessageService like every other chat; this is the people and the
// name: making the chat, the "Grup bilgisi" window, adding and removing people, leaving. Everything that changes is pushed
// live to the people it concerns ("ConversationAdded" / "ConversationUpdated" / "ConversationRemoved").
public interface IChatGroupService
{
    // Makes the chat with the friends picked (at least two, only friends of the maker, nobody blocked with them); the maker runs it.
    Task<GetConversationDto> CreateAsync(Guid currentUserId, PostChatGroupDto dto);

    // The "Grup bilgisi" window: members only.
    Task<GetChatGroupDto> GetAsync(Guid currentUserId, Guid conversationId);

    // Name and picture: people who run the chat.
    Task<GetChatGroupDto> UpdateAsync(Guid currentUserId, Guid conversationId, PutChatGroupDto dto);

    // Adds friends of the person who adds them: people who run the chat.
    Task<GetChatGroupDto> AddMembersAsync(Guid currentUserId, Guid conversationId, PostChatGroupMembersDto dto);

    // Takes somebody out: people who run the chat (not another one who runs it, and not themselves: that is "leave").
    Task<GetChatGroupDto> RemoveMemberAsync(Guid currentUserId, Guid conversationId, Guid memberId);

    // Gives or takes back the right to run the chat: people who run it (the chat always keeps at least one).
    Task<GetChatGroupDto> SetAdminAsync(Guid currentUserId, Guid conversationId, Guid memberId, bool isAdmin);

    // Leaves. If nobody who runs the chat is left, the member who has been in it longest does; the last one out deletes it.
    Task LeaveAsync(Guid currentUserId, Guid conversationId);

    // The person's own switch: no toasts, and the chat is left out of the unread total on the menu.
    Task<GetChatGroupDto> SetMutedAsync(Guid currentUserId, Guid conversationId, bool muted);
}
