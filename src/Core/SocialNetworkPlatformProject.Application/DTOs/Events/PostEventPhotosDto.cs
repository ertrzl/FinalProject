using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html "Fotoğraf ekle": one or several pictures in one request (the form field "photos" is repeated).
public class PostEventPhotosDto
{
    public List<IFormFile> Photos { get; set; } = new();
}
