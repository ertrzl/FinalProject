using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// F1: registration form (register.html) — full name, username, email, password, bio, avatar, birth date, location
public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Location { get; set; }
    public string? Bio { get; set; }
    public IFormFile? Avatar { get; set; }
}
