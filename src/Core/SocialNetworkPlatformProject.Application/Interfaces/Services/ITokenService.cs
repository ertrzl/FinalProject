namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure (SocialNetworkPlatformProject.Infrastructure.Services.TokenService).
// Takes plain primitives instead of ApplicationUser so Application never has to
// reference the Identity-based user type that lives in the Persistence layer.
public interface ITokenService
{
    // securityStamp is Identity's per-user "version of the credentials": it changes when the password does, and every
    // request compares the one in the token with the user's current one, so tokens issued before the change stop working.
    // sessionId is the session (one sign-in on one device) the token belongs to; it goes into the token as the "sid" claim so
    // that ending the session also ends its tokens.
    TokenResult GenerateAccessToken(Guid userId, string email, string fullName, string securityStamp, Guid sessionId, IEnumerable<string> roles);

    // The token of the photo cookie (MediaCookie): same user, security stamp and session, a different audience, and a longer
    // life (Jwt:MediaCookieHours) because a page that stays open keeps loading pictures between two renewals of the access token.
    // sessionId null = a token from before sessions existed.
    TokenResult GenerateMediaToken(Guid userId, string securityStamp, Guid? sessionId);

    // Opaque random string, not a JWT — just something to look up in the RefreshTokens table.
    string GenerateRefreshToken();
}
