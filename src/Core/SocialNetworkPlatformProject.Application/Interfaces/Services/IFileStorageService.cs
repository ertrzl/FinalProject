using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveImageAsync(IFormFile file, string folder);

    Task<string> SaveVideoAsync(IFormFile file, string folder);

    Task<string> CopyAsync(string url, string folder);

    void Delete(string? url);
}
