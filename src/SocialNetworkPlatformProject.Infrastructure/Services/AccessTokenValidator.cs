using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

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
        if (string.IsNullOrEmpty(tokenStamp))
            return false;

        if (!string.IsNullOrEmpty(sessionId) && _cache.TryGetValue(RevokedSessionKey(sessionId), out _))
            return false;

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

        _connections.CloseWithStamp(userId, securityStamp);
    }

    public void RevokeToken(string tokenId, DateTime expiresAtUtc)
    {
        var remaining = expiresAtUtc - DateTime.UtcNow + TimeSpan.FromMinutes(1);
        if (remaining <= TimeSpan.Zero)
            return;

        _cache.Set(RevokedKey(tokenId), true, remaining > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : remaining);
    }

    public void RevokeSession(Guid sessionId)
    {
        _cache.Set(RevokedSessionKey(sessionId.ToString()), true, TimeSpan.FromDays(1));
        _connections.CloseSession(sessionId);
    }

    private static string RevokedKey(string tokenId) => $"revoked-token:{tokenId}";

    private static string RevokedSessionKey(string sessionId) => $"revoked-session:{sessionId.ToLowerInvariant()}";

    private static string CacheKey(Guid userId, string securityStamp) => $"access-token:{userId}:{securityStamp}";
}
