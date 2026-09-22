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

    // Only take effect when the matching file above isn't provided — an uploaded file always wins.
    public bool RemoveAvatar { get; set; }
    public bool RemoveCoverPhoto { get; set; }
}
