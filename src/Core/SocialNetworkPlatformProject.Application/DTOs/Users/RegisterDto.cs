using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// F1: registration form (register.html) — full name, username, email, password, birth date, avatar.
// Bio/location are filled in later via PutUserProfileDto ("Profili Düzenle").
public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public IFormFile? Avatar { get; set; }
}
