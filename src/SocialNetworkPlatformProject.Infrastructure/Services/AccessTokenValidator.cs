using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// A JWT is valid until it expires — unless the account behind it is gone, or the password was changed since the token
// was issued. Every authenticated request asks this; a "yes" for a given (user, stamp) is remembered for a few
// seconds so it does not cost an extra query per request. When the password changes (or the account goes), the
// service that does it calls Revoke, which makes this forget the old stamp at once: the new token, with the new
// stamp, is checked against the database, and any token with the old stamp is refused from the next request on.
public class AccessTokenValidator : IAccessTokenRevoker
{
    private static readonly TimeSpan KnownValidFor = TimeSpan.FromSeconds(15);

    private readonly IMemoryCache _cache;
    private readonly IUserRepository _users;

    public AccessTokenValidator(IMemoryCache cache, IUserRepository users)
    {
        _cache = cache;
        _users = users;
    }

    public async Task<bool> IsValidAsync(Guid userId, string? tokenStamp)
    {
        // A token without the stamp was issued before this check existed: it has to be exchanged for a new one.
        if (string.IsNullOrEmpty(tokenStamp))
            return false;

        var key = CacheKey(userId, tokenStamp);
        if (_cache.TryGetValue(key, out _))
            return true;

        var currentStamp = await _users.GetSecurityStampAsync(userId);
        if (currentStamp == null || !string.Equals(currentStamp, tokenStamp, StringComparison.Ordinal))
            return false;

        _cache.Set(key, true, KnownValidFor);
        return true;
    }

    public void Revoke(Guid userId, string securityStamp)
    {
        _cache.Remove(CacheKey(userId, securityStamp));
    }

    private static string CacheKey(Guid userId, string securityStamp) => $"access-token:{userId}:{securityStamp}";
}
