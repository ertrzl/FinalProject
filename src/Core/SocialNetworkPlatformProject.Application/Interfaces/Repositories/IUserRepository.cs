namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserSummary?> GetSummaryAsync(Guid id);

    Task<Dictionary<Guid, UserSummary>> GetSummariesAsync(IEnumerable<Guid> ids);

    Task<Dictionary<string, UserSummary>> GetSummariesByUserNamesAsync(IEnumerable<string> userNames);

    Task<bool> ExistsAsync(Guid id);

    Task<string?> GetSecurityStampAsync(Guid id);

    Task<string?> GetLocationAsync(Guid id);

    Task<NotificationPreferences?> GetNotificationPreferencesAsync(Guid id);

    Task<(List<UserSummary> Items, int TotalCount)> SearchAsync(string term, Guid currentUserId, IReadOnlyCollection<Guid> excludeIds, int page, int pageSize);

    Task<List<UserSummary>> GetRecentAsync(IEnumerable<Guid> excludeIds, int take);

    Task TouchLastSeenAsync(Guid id);

    Task<int> CountAsync(bool? suspended = null, DateTime? createdSince = null);

    Task<(List<AdminUserRow> Items, int TotalCount)> SearchForAdminAsync(string? term, bool? suspended, int page, int pageSize);

    Task<List<string>> DeleteUserWithDataAsync(Guid userId);
}
