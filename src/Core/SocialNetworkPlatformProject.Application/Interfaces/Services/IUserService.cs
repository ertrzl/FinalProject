using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IUserService
{
    Task<GetUserProfileDto> GetProfileAsync(Guid userId, Guid currentUserId);

    Task<GetUserProfileDto> GetProfileByUserNameAsync(string userName, Guid currentUserId);

    Task<string?> FindUserNameAsync(string userName);

    Task<GetUserProfileDto> UpdateProfileAsync(Guid currentUserId, PutUserProfileDto dto);

    Task UpdatePrivacyAsync(Guid currentUserId, PutPrivacySettingsDto dto);

    Task<GetNotificationSettingsDto> GetNotificationSettingsAsync(Guid currentUserId);

    Task<GetNotificationSettingsDto> UpdateNotificationSettingsAsync(Guid currentUserId, PutNotificationSettingsDto dto);

    Task<TokenResponseDto> ChangePasswordAsync(Guid currentUserId, PutPasswordDto dto);

    Task DeleteAccountAsync(Guid currentUserId, DeleteAccountDto dto);

    Task<PagedResult<GetUserSearchResultDto>> SearchAsync(string term, Guid currentUserId, int page, int pageSize);

    Task UpdateLastSeenAsync(Guid userId);
}
