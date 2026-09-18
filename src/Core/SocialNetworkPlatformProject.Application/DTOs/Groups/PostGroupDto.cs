using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// "Grup Oluştur" modal on groups.html
public class PostGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Privacy { get; set; } = "Public"; // "Public" / "Private"
    public IFormFile? CoverImage { get; set; }
}
