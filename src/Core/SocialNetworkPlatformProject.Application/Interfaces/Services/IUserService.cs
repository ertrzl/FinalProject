using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IUserService
{
    Task<GetUserProfileDto> GetProfileAsync(Guid userId, Guid currentUserId);

    // The profile of the person with this user name (any letter case): the short address /u/<username> opens it. Unknown name = not found.
    Task<GetUserProfileDto> GetProfileByUserNameAsync(string userName, Guid currentUserId);

    // The user name as it is stored (right letter case) when somebody has it; null otherwise. Needs no sign-in: it only tells whether a short address leads somewhere.
    Task<string?> FindUserNameAsync(string userName);

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
