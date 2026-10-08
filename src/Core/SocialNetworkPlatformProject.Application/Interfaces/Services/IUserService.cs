using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IUserService
{
    Task<GetUserProfileDto> GetProfileAsync(Guid userId, Guid currentUserId);

    Task<GetUserProfileDto> UpdateProfileAsync(Guid currentUserId, PutUserProfileDto dto);

    Task UpdatePrivacyAsync(Guid currentUserId, PutPrivacySettingsDto dto);

    Task<GetNotificationSettingsDto> GetNotificationSettingsAsync(Guid currentUserId);

    // From now on the notification kinds the user switched off are no longer created for them (see NotificationPreferences).
    Task<GetNotificationSettingsDto> UpdateNotificationSettingsAsync(Guid currentUserId, PutNotificationSettingsDto dto);

    // Every other session is ended; the returned tokens keep the current device signed in.
    Task<TokenResponseDto> ChangePasswordAsync(Guid currentUserId, PutPasswordDto dto);

    Task DeleteAccountAsync(Guid currentUserId, DeleteAccountDto dto);

    Task<PagedResult<GetUserSearchResultDto>> SearchAsync(string term, Guid currentUserId, int page, int pageSize);

    Task UpdateLastSeenAsync(Guid userId);
}
