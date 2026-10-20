using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMessageService
{
    Task<List<GetConversationDto>> GetConversationsAsync(Guid currentUserId);

    // Page 1 = the newest messages; items inside a page are ordered oldest -> newest for display.
    Task<PagedResult<GetMessageDto>> GetMessagesAsync(Guid currentUserId, Guid conversationId, int page, int pageSize);

    // Persists the message, then pushes it to the other participant over SignalR.
    Task<GetMessageDto> SendAsync(Guid currentUserId, PostMessageDto dto);

    // Same as SendAsync for a photo (with an optional caption); the image is stored under uploads/messages.
    Task<GetMessageDto> SendImageAsync(Guid currentUserId, PostMessageImageDto dto);

    // "Delete for everyone": only the sender may take a message back. The text (and photo) is removed, both people
    // see a "deleted" bubble and every open tab of both is told ("MessageDeleted"). Deleting twice changes nothing.
    // Returns the message as the caller sees it now.
    Task<GetMessageDto> DeleteAsync(Guid currentUserId, Guid messageId);

    // Leaves an emoji (one of MessageReactions.Allowed) on a message of one of the user's conversations, replacing the
    // one they left before; the same emoji again, or none, takes it back. Not on a message that was taken back, and not
    // between people who blocked each other. Both people's open tabs are told ("MessageUpdated"). Returns the message
    // as the caller sees it now.
    Task<GetMessageDto> SetReactionAsync(Guid currentUserId, Guid messageId, string? emoji);

    // Also tells the other participant (read receipt) when something was actually marked read.
    Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId);

    // "X is typing..." signal for the other participant; nothing is stored.
    Task NotifyTypingAsync(Guid currentUserId, Guid conversationId);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    // May the user write to this person? Always inside an existing conversation; to start a new one the two must be
    // friends or the other person's account must not be private.
    Task<bool> CanMessageAsync(Guid currentUserId, Guid otherUserId);

    // ---- Group chats (Conversation.GroupId): one per group, its participants are the group's current members ----

    // group.html "Grup Sohbeti": the group's chat as the conversation list shows it (created on first use). Members only.
    Task<GetConversationDto> OpenGroupChatAsync(Guid currentUserId, Guid groupId);

    // Called by GroupService whenever someone becomes a member (join, approved request, accepted invite). Nothing to do
    // while the group has no chat yet: it is created with every member in it.
    Task AddToGroupChatAsync(Guid groupId, Guid userId);

    // Called by GroupService whenever someone stops being a member (left, removed). They lose the chat at once: no more
    // messages reach them, and their open tabs are told ("ConversationRemoved") so the chat closes.
    Task RemoveFromGroupChatAsync(Guid groupId, Guid userId);

    // The photos of a group's chat, to delete their files when the group (and with it the chat) is deleted.
    Task<List<string>> GetGroupChatFilesAsync(Guid groupId);

    // One conversation as the list shows it to this person (they must be a participant).
    Task<GetConversationDto> GetConversationAsync(Guid currentUserId, Guid conversationId);
}
