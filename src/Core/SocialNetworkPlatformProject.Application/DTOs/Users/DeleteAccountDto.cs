namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Hesabı Sil" dialog: the account is only deleted after the password is typed again.
public class DeleteAccountDto
{
    public string Password { get; set; } = string.Empty;
}
