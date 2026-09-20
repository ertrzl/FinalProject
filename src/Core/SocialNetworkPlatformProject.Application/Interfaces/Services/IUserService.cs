using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IUserService
{
    Task<GetUserProfileDto> GetProfileAsync(Guid userId, Guid currentUserId);

    Task<GetUserProfileDto> UpdateProfileAsync(Guid currentUserId, PutUserProfileDto dto);

    Task UpdatePrivacyAsync(Guid currentUserId, PutPrivacySettingsDto dto);

    Task ChangePasswordAsync(Guid currentUserId, PutPasswordDto dto);

    Task DeleteAccountAsync(Guid currentUserId);

    Task<PagedResult<GetUserSearchResultDto>> SearchAsync(string term, Guid currentUserId, int page, int pageSize);

    Task UpdateLastSeenAsync(Guid userId);
}
