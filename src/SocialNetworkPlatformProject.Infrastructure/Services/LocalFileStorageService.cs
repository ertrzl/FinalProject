using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 25 * 1024 * 1024;
    private const string UploadsFolder = "uploads";

    // How a picture is stored (see SaveResizedImageAsync): the longest side in pixels, the compression quality (0-100), and
    // the most pixels an uploaded picture may be made of (a small file can still hold a huge picture that needs a lot of
    // memory to open; 64 million is above the 48-megapixel photo of a phone).
    private const int MaxImageSide = 1600;
    private const int MaxAvatarSide = 512;
    private const int JpegQuality = 82;
    private const int WebpQuality = 80;
    private const long MaxPixels = 64_000_000;

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
        var fullPath = Path.Combine(directory, fileName);

        // A picture is made smaller first (see SaveResizedImageAsync); an animated GIF, or a video, is kept as it came.
        if (ReferenceEquals(allowedExtensions, AllowedImageExtensions) && !extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await SaveResizedImageAsync(file, fullPath, extension, folder);
            }
            catch
            {
                // Nothing half written stays behind.
                if (File.Exists(fullPath)) File.Delete(fullPath);
                throw;
            }
        }
        else
        {
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        _logger.LogDebug("Saved upload {Folder}/{FileName} ({Bytes} bytes sent, {Stored} stored)", folder, fileName, file.Length, new FileInfo(fullPath).Length);
        return $"/{UploadsFolder}/{folder}/{fileName}";
    }

    // The longest side a stored picture may have. A profile picture is shown small, everything else at most about this wide.
    private static int MaxSideFor(string folder) => folder == "avatars" ? MaxAvatarSide : MaxImageSide;

    // Saves the picture turned the right way up, no wider or taller than MaxSideFor(folder), and compressed. Opening and
    // saving the picture again also leaves out the extra data a camera adds (where it was taken, the phone's name). A phone
    // photo of 4 MB ends up at a few hundred KB: the page shows it just as sharp, but it downloads, and fills the disk, many
    // times less. A picture that cannot be read as a picture, or is made of an unreasonable number of pixels, is refused.
    // An animated picture is stored as it is.
    private async Task SaveResizedImageAsync(IFormFile file, string fullPath, string extension, string folder)
    {
        // The picture is read into memory (the upload is at most 5 MB), so it can be read twice: once for its size, once for its pixels.
        using var buffer = new MemoryStream((int)file.Length);
        await using (var upload = file.OpenReadStream())
            await upload.CopyToAsync(buffer);

        buffer.Position = 0;
        using var codec = SKCodec.Create(buffer);
        if (codec == null)
        {
            _logger.LogWarning("Upload refused: {Folder} picture cannot be read", folder);
            throw new BadRequestException(Loc.T("file.imageType"));
        }

        if ((long)codec.Info.Width * codec.Info.Height > MaxPixels)
        {
            _logger.LogWarning("Upload refused: {Folder} picture of {Width}x{Height} pixels", folder, codec.Info.Width, codec.Info.Height);
            throw new BadRequestException(Loc.T("file.imagePixels"));
        }

        if (codec.FrameCount > 1)
        {
            await File.WriteAllBytesAsync(fullPath, buffer.ToArray());
            return;
        }

        using var decoded = SKBitmap.Decode(codec);
        if (decoded == null)
        {
            _logger.LogWarning("Upload refused: {Folder} picture cannot be decoded", folder);
            throw new BadRequestException(Loc.T("file.imageType"));
        }

        using var upright = TurnUpright(decoded, codec.EncodedOrigin);
        var maxSide = MaxSideFor(folder);
        var longest = Math.Max(upright.Width, upright.Height);

        SKBitmap? smaller = null;
        try
        {
            if (longest > maxSide)
            {
                var scale = (double)maxSide / longest;
                var info = new SKImageInfo(Math.Max(1, (int)Math.Round(upright.Width * scale)), Math.Max(1, (int)Math.Round(upright.Height * scale)),
                    upright.ColorType, upright.AlphaType);
                smaller = upright.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
            }

            var format = extension.ToLowerInvariant() switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
            var quality = format == SKEncodedImageFormat.Webp ? WebpQuality : JpegQuality;

            using var image = SKImage.FromBitmap(smaller ?? upright);
            using var data = image.Encode(format, quality);
            if (data == null)
                throw new BadRequestException(Loc.T("file.imageType"));

            await using var output = new FileStream(fullPath, FileMode.Create);
            data.SaveTo(output);
        }
        finally
        {
            smaller?.Dispose();
        }
    }

    // A camera stores a photo as the sensor saw it and notes how it has to be turned. The pictures are saved without that
    // note, so the turn is done here: a photo taken upright but stored sideways stays upright.
    private static SKBitmap TurnUpright(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default)
            return source.Copy();

        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsSides ? source.Height : source.Width;
        var height = swapsSides ? source.Width : source.Height;
        var result = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));

        using var canvas = new SKCanvas(result);
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, source.Width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, source.Width, 0, -1, source.Height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, source.Height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, source.Height, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, source.Height, -1, 0, source.Width, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, source.Width, 0, 0, 1),
            _ => SKMatrix.Identity
        };
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0);
        return result;
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
