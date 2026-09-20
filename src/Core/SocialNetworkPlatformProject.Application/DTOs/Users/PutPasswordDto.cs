namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Şifre Değiştir" section
public class PutPasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
