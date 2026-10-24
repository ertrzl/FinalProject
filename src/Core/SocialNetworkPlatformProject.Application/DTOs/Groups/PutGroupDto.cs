using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class PutGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Privacy { get; set; } = "Public";
    public IFormFile? CoverImage { get; set; }
    public bool RemoveCoverImage { get; set; }
    public IFormFile? Avatar { get; set; }
    public bool RemoveAvatar { get; set; }
}
