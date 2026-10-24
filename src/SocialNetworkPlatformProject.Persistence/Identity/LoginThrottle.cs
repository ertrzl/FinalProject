using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Exceptions;

namespace SocialNetworkPlatformProject.Persistence.Identity;

public class LoginThrottle
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockedFor = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public LoginThrottle(IMemoryCache cache)
    {
        _cache = cache;
    }

    private sealed class Attempts
    {
        public int Failures;
        public DateTime? LockedUntilUtc;
    }

    public void EnsureNotLocked(string identifier)
    {
        if (!_cache.TryGetValue(Key(identifier), out Attempts? attempts) || attempts == null)
            return;

        lock (attempts)
        {
            if (attempts.LockedUntilUtc > DateTime.UtcNow)
                throw TooMany(attempts.LockedUntilUtc.Value);
        }
    }

    public void RecordFailure(string identifier)
    {
        var key = Key(identifier);
        var attempts = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LockedFor;
            return new Attempts();
        })!;

        lock (attempts)
        {
            attempts.Failures++;

            if (attempts.Failures >= MaxFailures && attempts.LockedUntilUtc == null)
            {
                attempts.LockedUntilUtc = DateTime.UtcNow + LockedFor;
                _cache.Set(key, attempts, LockedFor);
            }

            if (attempts.LockedUntilUtc > DateTime.UtcNow)
                throw TooMany(attempts.LockedUntilUtc.Value);
        }
    }

    public void Forget(string identifier) => _cache.Remove(Key(identifier));

    private static TooManyRequestsException TooMany(DateTime lockedUntilUtc)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling((lockedUntilUtc - DateTime.UtcNow).TotalMinutes));
        return new TooManyRequestsException(Loc.T("auth.tooManyAttempts", minutes));
    }

    private static string Key(string identifier) =>
        "login-throttle:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim().ToLowerInvariant())));
}
