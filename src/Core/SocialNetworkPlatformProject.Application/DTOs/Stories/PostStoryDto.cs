using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// "Hikaye Ekle" modal on home.html — accepts either a photo or a short video.
public class PostStoryDto
{
    public IFormFile Media { get; set; } = null!;
}
