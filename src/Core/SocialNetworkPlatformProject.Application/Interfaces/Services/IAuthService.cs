using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto dto);

    Task<TokenResponseDto> LoginAsync(LoginDto dto);

    // Exchanges a still-valid refresh token for a new access token, rotating the refresh token in the process.
    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

    // Revokes a refresh token so it can no longer be used (called on logout).
    Task RevokeRefreshTokenAsync(RefreshTokenDto dto);
}
