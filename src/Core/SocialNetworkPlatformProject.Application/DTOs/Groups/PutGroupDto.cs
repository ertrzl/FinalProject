using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// group.html's admin-only "Grubu Düzenle" modal.
public class PutGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Privacy { get; set; } = "Public"; // "Public" / "Private"
    public IFormFile? CoverImage { get; set; }
    public bool RemoveCoverImage { get; set; }
}
