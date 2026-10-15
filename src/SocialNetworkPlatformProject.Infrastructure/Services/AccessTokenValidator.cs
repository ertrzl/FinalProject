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
    private readonly HubConnectionRegistry _connections;

    public AccessTokenValidator(IMemoryCache cache, IUserRepository users, HubConnectionRegistry connections)
    {
        _cache = cache;
        _users = users;
        _connections = connections;
    }

    public async Task<bool> IsValidAsync(Guid userId, string? tokenStamp, string? tokenId = null, string? sessionId = null)
    {
        // A token without the stamp was issued before this check existed: it has to be exchanged for a new one.
        if (string.IsNullOrEmpty(tokenStamp))
            return false;

        // A session that was ended from the settings page (RevokeSession). A token without a session is from before sessions
        // existed: nothing to look up.
        if (!string.IsNullOrEmpty(sessionId) && _cache.TryGetValue(RevokedSessionKey(sessionId), out _))
            return false;

        // A token that was signed out (RevokeToken) is refused until it would have expired anyway.
        if (!string.IsNullOrEmpty(tokenId) && _cache.TryGetValue(RevokedKey(tokenId), out _))
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

        // The live connections made with that stamp stop here too; the ones that only listen (notifications) would otherwise
        // keep receiving pushes until the person closed the tab.
        _connections.CloseWithStamp(userId, securityStamp);
    }

    public void RevokeToken(string tokenId, DateTime expiresAtUtc)
    {
        // Remembered only as long as the token itself could still be used (a minute more for the clock skew allowed in the
        // validation), and never for longer than a day, whatever the token claims.
        var remaining = expiresAtUtc - DateTime.UtcNow + TimeSpan.FromMinutes(1);
        if (remaining <= TimeSpan.Zero)
            return;

        _cache.Set(RevokedKey(tokenId), true, remaining > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : remaining);
    }

    public void RevokeSession(Guid sessionId)
    {
        // Remembered for a day, longer than any token of the session can live (the photo cookie is the longest).
        _cache.Set(RevokedSessionKey(sessionId.ToString()), true, TimeSpan.FromDays(1));
        _connections.CloseSession(sessionId);
    }

    private static string RevokedKey(string tokenId) => $"revoked-token:{tokenId}";

    private static string RevokedSessionKey(string sessionId) => $"revoked-session:{sessionId.ToLowerInvariant()}";

    private static string CacheKey(Guid userId, string securityStamp) => $"access-token:{userId}:{securityStamp}";
}
