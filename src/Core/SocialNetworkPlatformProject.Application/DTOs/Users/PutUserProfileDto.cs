using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// settings.html "Hesap Bilgileri" section + profile.html "Profili Düzenle" modal
public class PutUserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public IFormFile? Avatar { get; set; }
    public IFormFile? CoverPhoto { get; set; }
}
