using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IChatGroupService
{
    Task<GetConversationDto> CreateAsync(Guid currentUserId, PostChatGroupDto dto);

    Task<GetChatGroupDto> GetAsync(Guid currentUserId, Guid conversationId);

    Task<GetChatGroupDto> UpdateAsync(Guid currentUserId, Guid conversationId, PutChatGroupDto dto);

    Task<GetChatGroupDto> AddMembersAsync(Guid currentUserId, Guid conversationId, PostChatGroupMembersDto dto);

    Task<GetChatGroupDto> RemoveMemberAsync(Guid currentUserId, Guid conversationId, Guid memberId);

    Task<GetChatGroupDto> SetAdminAsync(Guid currentUserId, Guid conversationId, Guid memberId, bool isAdmin);

    Task LeaveAsync(Guid currentUserId, Guid conversationId);

    Task<GetChatGroupDto> SetMutedAsync(Guid currentUserId, Guid conversationId, bool muted);
}
