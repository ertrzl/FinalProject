namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

// Not an IRepository<T>: users are ASP.NET Identity's ApplicationUser (Persistence), not a BaseEntity.
public interface IUserRepository
{
    Task<UserSummary?> GetSummaryAsync(Guid id);

    Task<Dictionary<Guid, UserSummary>> GetSummariesAsync(IEnumerable<Guid> ids);

    // Users by user name, in any letter case; the key is the lower-case user name. Unknown names are simply missing.
    Task<Dictionary<string, UserSummary>> GetSummariesByUserNamesAsync(IEnumerable<string> userNames);

    Task<bool> ExistsAsync(Guid id);

    // The user's current security stamp; null when there is no such user.
    Task<string?> GetSecurityStampAsync(Guid id);

    // What the user chose to be notified about; null when there is no such user.
    Task<NotificationPreferences?> GetNotificationPreferencesAsync(Guid id);

    // Includes the current user in results — typing your own name should still find your own profile.
    // The excluded ids (the people blocked either way) never show up.
    Task<(List<UserSummary> Items, int TotalCount)> SearchAsync(string term, Guid currentUserId, IReadOnlyCollection<Guid> excludeIds, int page, int pageSize);

    // Newest users first, skipping the given ids — used to pad friend suggestions.
    Task<List<UserSummary>> GetRecentAsync(IEnumerable<Guid> excludeIds, int take);

    Task TouchLastSeenAsync(Guid id);

    // Accounts in all, or only the suspended ones / the ones made since the given time (the admin panel's numbers).
    Task<int> CountAsync(bool? suspended = null, DateTime? createdSince = null);

    // The admin panel's user list, newest first; `term` looks in name, user name and e-mail.
    Task<(List<AdminUserRow> Items, int TotalCount)> SearchForAdminAsync(string? term, bool? suspended, int page, int pageSize);

    // Removes the account together with everything it owns or took part in (posts, comments, likes, friendships,
    // messages, login sessions, ...) in one transaction: it either all goes or nothing does.
    // Returns the uploaded-image URLs of the deleted rows (profile photos included) so the caller can remove the files too.
    Task<List<string>> DeleteUserWithDataAsync(Guid userId);
}
