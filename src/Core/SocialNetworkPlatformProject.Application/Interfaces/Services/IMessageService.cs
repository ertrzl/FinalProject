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

    Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId);

    Task<int> GetUnreadCountAsync(Guid currentUserId);
}
