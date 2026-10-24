namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IAccessTokenRevoker
{
    void Revoke(Guid userId, string securityStamp);

    void RevokeToken(string tokenId, DateTime expiresAtUtc);

    void RevokeSession(Guid sessionId);
}
