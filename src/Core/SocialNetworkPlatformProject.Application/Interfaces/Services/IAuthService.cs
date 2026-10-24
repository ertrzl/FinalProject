using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto dto);

    Task<TokenResponseDto> LoginAsync(LoginDto dto);

    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

    Task RevokeRefreshTokenAsync(RefreshTokenDto dto);
}
