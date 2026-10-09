using SocialNetworkPlatformProject.Application.Common;
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

    // Also tells the other participant (read receipt) when something was actually marked read.
    Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId);

    // "X is typing..." signal for the other participant; nothing is stored.
    Task NotifyTypingAsync(Guid currentUserId, Guid conversationId);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    // May the user write to this person? Always inside an existing conversation; to start a new one the two must be
    // friends or the other person's account must not be private.
    Task<bool> CanMessageAsync(Guid currentUserId, Guid otherUserId);
}
