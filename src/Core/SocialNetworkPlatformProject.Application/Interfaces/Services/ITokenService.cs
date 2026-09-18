namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure (SocialNetworkPlatformProject.Infrastructure.Services.TokenService).
// Takes plain primitives instead of ApplicationUser so Application never has to
// reference the Identity-based user type that lives in the Persistence layer.
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName, IEnumerable<string> roles);
}
