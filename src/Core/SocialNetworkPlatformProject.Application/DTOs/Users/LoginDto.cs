namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// index.html login form — accepts either email or username in the same field
public class LoginDto
{
    public string EmailOrUserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
