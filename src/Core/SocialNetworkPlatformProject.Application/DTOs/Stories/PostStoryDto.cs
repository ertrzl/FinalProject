using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// "Hikaye Ekle" modal on home.html
public class PostStoryDto
{
    public IFormFile Image { get; set; } = null!;
}
