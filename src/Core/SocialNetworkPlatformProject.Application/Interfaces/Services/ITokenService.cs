using SocialNetworkPlatformProject.Application.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure (SocialNetworkPlatformProject.Infrastructure.Services.TokenService).
// Takes plain primitives instead of ApplicationUser so Application never has to
// reference the Identity-based user type that lives in the Persistence layer.
public interface ITokenService
{
    // securityStamp is Identity's per-user "version of the credentials": it changes when the password does, and every
    // request compares the one in the token with the user's current one, so tokens issued before the change stop working.
    TokenResult GenerateAccessToken(Guid userId, string email, string fullName, string securityStamp, IEnumerable<string> roles);

    // Opaque random string, not a JWT — just something to look up in the RefreshTokens table.
    string GenerateRefreshToken();
}
