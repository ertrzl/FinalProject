using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string UploadsFolder = "uploads";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private readonly string _webRootPath;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file.Length == 0)
            throw new BadRequestException("The uploaded file is empty.");

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException("Images can be at most 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Only JPG, PNG, GIF and WEBP images are allowed.");

        var directory = Path.Combine(_webRootPath, UploadsFolder, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var stream = new FileStream(Path.Combine(directory, fileName), FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/{UploadsFolder}/{folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith($"/{UploadsFolder}/", StringComparison.Ordinal))
            return;

        var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_webRootPath, relativePath));

        // Guard against a crafted URL escaping the uploads folder.
        var uploadsRoot = Path.GetFullPath(Path.Combine(_webRootPath, UploadsFolder)) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
