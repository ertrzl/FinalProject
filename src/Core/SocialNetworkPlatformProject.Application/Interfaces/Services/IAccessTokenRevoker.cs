namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The part of the app that checks access tokens remembers, for a few seconds, that a user's token was fine (so it does
// not ask the database on every request). When a user's tokens must stop working right now — the password was
// changed, the account was deleted — this makes it forget, so the very next request with an old token is refused.
// Implemented in Infrastructure (AccessTokenValidator).
public interface IAccessTokenRevoker
{
    // Also closes the live (SignalR) connections that were made with a token of that stamp.
    void Revoke(Guid userId, string securityStamp);

    // One single token (its "jti") stops working at once, until it would have expired anyway: what signing out does, so
    // a token that was copied before that is of no use. The person's other tokens are not touched.
    void RevokeToken(string tokenId, DateTime expiresAtUtc);

    // Every token of one session (access tokens and photo cookie, whichever of them are still out there) stops working at
    // once, and the live connections made in it are closed: what "end this session" in the settings page does.
    void RevokeSession(Guid sessionId);
}
