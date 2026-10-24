namespace SocialNetworkPlatformProject.Application.DTOs.Users;

public class RegisterResultDto
{
    public bool EmailVerificationRequired { get; set; }
    public string Email { get; set; } = string.Empty;
    public TokenResponseDto? Session { get; set; }
}
