using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class PostEventPhotosDto
{
    public List<IFormFile> Photos { get; set; } = new();
}
