using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEmailVerificationService
{
    bool IsRequired { get; }

    Task SendLinkAsync(Guid userId, string? linkBaseUrl);

    Task ResendAsync(ResendVerificationDto dto, string? linkBaseUrl);

    Task VerifyAsync(VerifyEmailDto dto);
}
