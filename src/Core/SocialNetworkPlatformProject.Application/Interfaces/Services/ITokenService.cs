namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ITokenService
{
    TokenResult GenerateAccessToken(Guid userId, string email, string fullName, string securityStamp, Guid sessionId, IEnumerable<string> roles);

    TokenResult GenerateMediaToken(Guid userId, string securityStamp, Guid? sessionId);

    string GenerateRefreshToken();
}
