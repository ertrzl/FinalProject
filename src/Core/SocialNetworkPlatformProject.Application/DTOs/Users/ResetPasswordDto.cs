namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// reset-password.html: the e-mail and code from the link, and the new password typed twice
public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
