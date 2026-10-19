using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure (local wwwroot/uploads folder).
public interface IFileStorageService
{
    // Validates the image and returns its public URL (e.g. "/uploads/posts/abc.jpg").
    Task<string> SaveImageAsync(IFormFile file, string folder);

    // Same as SaveImageAsync but for short video clips (stories); larger size limit, different allowed types.
    Task<string> SaveVideoAsync(IFormFile file, string folder);

    // Makes a copy of an uploaded file (url from this store) in another folder and returns the copy's URL, so a copy can outlive the original.
    Task<string> CopyAsync(string url, string folder);

    void Delete(string? url);
}
