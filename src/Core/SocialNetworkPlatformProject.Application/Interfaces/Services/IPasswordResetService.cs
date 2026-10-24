using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPasswordResetService
{
    Task RequestAsync(ForgotPasswordDto dto, string? linkBaseUrl);

    Task ResetAsync(ResetPasswordDto dto);
}
