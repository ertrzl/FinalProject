using SocialNetworkPlatformProject.Application.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

// Not an IRepository<T>: users are ASP.NET Identity's ApplicationUser (Persistence), not a BaseEntity.
public interface IUserRepository
{
    Task<UserSummary?> GetSummaryAsync(Guid id);

    Task<Dictionary<Guid, UserSummary>> GetSummariesAsync(IEnumerable<Guid> ids);

    Task<bool> ExistsAsync(Guid id);

    Task<(List<UserSummary> Items, int TotalCount)> SearchAsync(string term, Guid excludeUserId, int page, int pageSize);

    // Newest users first, skipping the given ids — used to pad friend suggestions.
    Task<List<UserSummary>> GetRecentAsync(IEnumerable<Guid> excludeIds, int take);

    Task TouchLastSeenAsync(Guid id);

    // Removes everything the user owns or took part in (posts, comments, likes, friendships, messages, ...).
    // Returns the uploaded-image URLs of the deleted rows so the caller can remove the files too.
    Task<List<string>> DeleteAllUserDataAsync(Guid userId);
}
