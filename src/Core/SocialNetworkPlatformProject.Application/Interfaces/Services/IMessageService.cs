using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMessageService
{
    Task<List<GetConversationDto>> GetConversationsAsync(Guid currentUserId);

    Task<PagedResult<GetMessageDto>> GetMessagesAsync(Guid currentUserId, Guid conversationId, int page, int pageSize);

    Task<GetMessageDto> SendAsync(Guid currentUserId, PostMessageDto dto);

    Task<GetMessageDto> SendImageAsync(Guid currentUserId, PostMessageImageDto dto);

    Task<GetMessageDto> DeleteAsync(Guid currentUserId, Guid messageId);

    Task<GetMessageDto> SetReactionAsync(Guid currentUserId, Guid messageId, string? emoji);

    Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId);

    Task NotifyTypingAsync(Guid currentUserId, Guid conversationId);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    Task<bool> CanMessageAsync(Guid currentUserId, Guid otherUserId);


    Task<GetConversationDto> OpenGroupChatAsync(Guid currentUserId, Guid groupId);

    Task AddToGroupChatAsync(Guid groupId, Guid userId);

    Task RemoveFromGroupChatAsync(Guid groupId, Guid userId);

    Task<List<string>> GetGroupChatFilesAsync(Guid groupId);

    Task<GetConversationDto> GetConversationAsync(Guid currentUserId, Guid conversationId);
}
