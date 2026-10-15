using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Exceptions;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// Identity locks an ACCOUNT after five wrong passwords (PasswordVerifier). If a name that is not an account never got locked,
// the answer to the sixth guess would tell whoever is guessing whether the account exists: "too many attempts" for a real
// one, "wrong password" for an invented one. So a name that matches no account is counted here, in memory, and locked
// for the same time after the same number of wrong tries, with the same message. (An account's own count lives in the
// database; this one is forgotten when the application restarts, which only gives a guesser a fresh start with names
// that do not exist anyway.)
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

    // Throws the same 429 as a locked account while this name is locked.
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

    // One more wrong try for a name that is no account. The try that reaches the limit already answers 429 (as an account's does).
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
                _cache.Set(key, attempts, LockedFor); // the lock lasts a full LockedFor from now on
            }

            if (attempts.LockedUntilUtc > DateTime.UtcNow)
                throw TooMany(attempts.LockedUntilUtc.Value);
        }
    }

    // A name that has just become an account (someone registered with it) starts clean.
    public void Forget(string identifier) => _cache.Remove(Key(identifier));

    private static TooManyRequestsException TooMany(DateTime lockedUntilUtc)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling((lockedUntilUtc - DateTime.UtcNow).TotalMinutes));
        return new TooManyRequestsException(Loc.T("auth.tooManyAttempts", minutes));
    }

    // Hashed, so a name of any length (or a few million of them) takes the same small space.
    private static string Key(string identifier) =>
        "login-throttle:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim().ToLowerInvariant())));
}
