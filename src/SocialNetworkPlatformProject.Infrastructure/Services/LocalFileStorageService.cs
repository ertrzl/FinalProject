using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 25 * 1024 * 1024;
    private const string UploadsFolder = "uploads";

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov"
    };

    private readonly string _webRootPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    public Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        return SaveAsync(file, folder, MaxImageSizeBytes, AllowedImageExtensions, "image/",
            Loc.T("file.imageType"), Loc.T("file.imageSize"));
    }

    public Task<string> SaveVideoAsync(IFormFile file, string folder)
    {
        return SaveAsync(file, folder, MaxVideoSizeBytes, AllowedVideoExtensions, "video/",
            Loc.T("file.videoType"), Loc.T("file.videoSize"));
    }

    private async Task<string> SaveAsync(
        IFormFile file, string folder, long maxSizeBytes, HashSet<string> allowedExtensions,
        string requiredContentTypePrefix, string typeErrorMessage, string sizeErrorMessage)
    {
        if (file.Length == 0)
            throw new BadRequestException(Loc.T("file.empty"));

        if (file.Length > maxSizeBytes)
        {
            _logger.LogInformation("Upload refused: {Folder} file of {Bytes} bytes is over the limit of {Limit}", folder, file.Length, maxSizeBytes);
            throw new BadRequestException(sizeErrorMessage);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension) || !file.ContentType.StartsWith(requiredContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Upload refused: {Folder} file with type {ContentType} and extension {Extension}", folder, file.ContentType, extension);
            throw new BadRequestException(typeErrorMessage);
        }

        // The first bytes have to be those of the kind of file the name claims to be.
        if (!await FileSignatures.MatchesAsync(file, extension))
        {
            _logger.LogWarning("Upload refused: {Folder} file {Extension} does not start like one", folder, extension);
            throw new BadRequestException(typeErrorMessage);
        }

        var directory = Path.Combine(_webRootPath, UploadsFolder, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var stream = new FileStream(Path.Combine(directory, fileName), FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogDebug("Saved upload {Folder}/{FileName} ({Bytes} bytes)", folder, fileName, file.Length);
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
        {
            _logger.LogWarning("Delete refused: {Url} points outside the uploads folder", url);
            return;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted upload {Url}", url);
        }
    }
}
