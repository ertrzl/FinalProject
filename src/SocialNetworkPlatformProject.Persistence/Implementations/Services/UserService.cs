using Microsoft.AspNetCore.Identity;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IFileStorageService _files;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IUserRepository users,
        IFriendService friends,
        IFileStorageService files)
    {
        _userManager = userManager;
        _users = users;
        _friends = friends;
        _files = files;
    }

    public async Task<GetUserProfileDto> GetProfileAsync(Guid userId, Guid currentUserId)
    {
        var user = await _users.GetSummaryAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var isOwnProfile = userId == currentUserId;
        var friendCount = (await _friends.GetFriendIdsAsync(userId)).Count;

        var friendshipStatus = "None";
        if (!isOwnProfile)
        {
            var statuses = await _friends.GetFriendshipStatusesAsync(currentUserId, new[] { userId });
            friendshipStatus = statuses[userId];
        }

        return new GetUserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            AvatarUrl = user.AvatarUrl,
            CoverPhotoUrl = user.CoverPhotoUrl,
            Bio = user.Bio,
            Location = user.Location,
            Occupation = user.Occupation,
            Education = user.Education,
            JoinedAt = user.CreatedAt,
            FriendCount = friendCount,
            IsOnline = user.IsOnline,
            IsPrivateAccount = user.IsPrivateAccount,
            IsOwnProfile = isOwnProfile,
            FriendshipStatus = friendshipStatus
        };
    }

    public async Task<GetUserProfileDto> UpdateProfileAsync(Guid currentUserId, PutUserProfileDto dto)
    {
        var user = await FindUserAsync(currentUserId);

        user.FullName = dto.FullName.Trim();
        user.Bio = dto.Bio?.Trim();
        user.Location = dto.Location?.Trim();
        user.Occupation = dto.Occupation?.Trim();
        user.Education = dto.Education?.Trim();

        string? oldAvatar = null;
        string? oldCover = null;

        if (dto.Avatar != null)
        {
            oldAvatar = user.AvatarUrl;
            user.AvatarUrl = await _files.SaveImageAsync(dto.Avatar, "avatars");
        }
        else if (dto.RemoveAvatar)
        {
            oldAvatar = user.AvatarUrl;
            user.AvatarUrl = null;
        }

        if (dto.CoverPhoto != null)
        {
            oldCover = user.CoverPhotoUrl;
            user.CoverPhotoUrl = await _files.SaveImageAsync(dto.CoverPhoto, "covers");
        }
        else if (dto.RemoveCoverPhoto)
        {
            oldCover = user.CoverPhotoUrl;
            user.CoverPhotoUrl = null;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(JoinErrors(result));

        _files.Delete(oldAvatar);
        _files.Delete(oldCover);

        return await GetProfileAsync(currentUserId, currentUserId);
    }

    public async Task UpdatePrivacyAsync(Guid currentUserId, PutPrivacySettingsDto dto)
    {
        var user = await FindUserAsync(currentUserId);

        user.IsPrivateAccount = dto.IsPrivateAccount;
        user.ShowOnlineStatus = dto.ShowOnlineStatus;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(JoinErrors(result));
    }

    public async Task ChangePasswordAsync(Guid currentUserId, PutPasswordDto dto)
    {
        var user = await FindUserAsync(currentUserId);

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(JoinErrors(result));
    }

    public async Task DeleteAccountAsync(Guid currentUserId)
    {
        var user = await FindUserAsync(currentUserId);

        var orphanedFiles = await _users.DeleteAllUserDataAsync(currentUserId);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(JoinErrors(result));

        foreach (var file in orphanedFiles)
            _files.Delete(file);

        _files.Delete(user.AvatarUrl);
        _files.Delete(user.CoverPhotoUrl);
    }

    public async Task<PagedResult<GetUserSearchResultDto>> SearchAsync(string term, Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        term = term?.Trim() ?? string.Empty;
        if (term.Length == 0)
            return new PagedResult<GetUserSearchResultDto> { Page = page, PageSize = pageSize };

        var (found, total) = await _users.SearchAsync(term, currentUserId, page, pageSize);

        var ids = found.Select(u => u.Id).ToList();
        var statuses = await _friends.GetFriendshipStatusesAsync(currentUserId, ids);
        var mutualCounts = await _friends.GetMutualFriendCountsAsync(currentUserId, ids);

        var items = found.Select(u => new GetUserSearchResultDto
        {
            Id = u.Id,
            FullName = u.FullName,
            UserName = u.UserName,
            AvatarUrl = u.AvatarUrl,
            MutualFriendsCount = mutualCounts.GetValueOrDefault(u.Id),
            FriendshipStatus = statuses.GetValueOrDefault(u.Id, "None")
        }).ToList();

        return new PagedResult<GetUserSearchResultDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task UpdateLastSeenAsync(Guid userId)
    {
        await _users.TouchLastSeenAsync(userId);
    }

    private async Task<ApplicationUser> FindUserAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");
    }

    private static string JoinErrors(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(e => e.Description));
    }
}
