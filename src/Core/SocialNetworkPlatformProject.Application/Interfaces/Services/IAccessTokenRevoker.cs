namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The part of the app that checks access tokens remembers, for a few seconds, that a user's token was fine (so it does
// not ask the database on every request). When a user's tokens must stop working right now — the password was
// changed, the account was deleted — this makes it forget, so the very next request with an old token is refused.
// Implemented in Infrastructure (AccessTokenValidator).
public interface IAccessTokenRevoker
{
    void Revoke(Guid userId, string securityStamp);
}
