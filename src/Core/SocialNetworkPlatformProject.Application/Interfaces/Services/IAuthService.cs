using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IAuthService
{
    Task<RegisterResultDto> RegisterAsync(RegisterDto dto, string? linkBaseUrl);

    Task<TokenResponseDto> LoginAsync(LoginDto dto);

    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

    Task RevokeRefreshTokenAsync(RefreshTokenDto dto);
}
