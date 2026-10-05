using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// A JWT stays valid until it expires, even if its owner deleted the account in the meantime. Every authenticated
// request asks this whether the user still exists; "yes" is remembered for a few seconds so it is not one extra
// query per request.
public class ActiveUserChecker
{
    private static readonly TimeSpan KnownActiveFor = TimeSpan.FromSeconds(15);

    private readonly IMemoryCache _cache;
    private readonly IUserRepository _users;

    public ActiveUserChecker(IMemoryCache cache, IUserRepository users)
    {
        _cache = cache;
        _users = users;
    }

    public async Task<bool> IsActiveAsync(Guid userId)
    {
        var key = $"active-user:{userId}";
        if (_cache.TryGetValue(key, out _))
            return true;

        if (!await _users.ExistsAsync(userId))
            return false;

        _cache.Set(key, true, KnownActiveFor);
        return true;
    }
}
