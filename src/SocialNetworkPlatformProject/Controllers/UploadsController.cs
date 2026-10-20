using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// Uploaded photos and videos. They are not public files: the browser sends the photo cookie (MediaCookie) with every
// picture it loads, and a picture is only given to someone who may see the thing it belongs to (IUploadAccessService).
// A picture the person may not see is answered like one that does not exist.
[ApiController]
[Route("uploads")]
[Authorize(AuthenticationSchemes = MediaCookie.Scheme)]
public class UploadsController : ControllerBase
{
    // Only what LocalFileStorageService writes: <folder>/<32 hex digits>.<extension>. Nothing else is looked up on the disk.
    private static readonly Regex FilePattern = new(
        @"^(?<folder>avatars|covers|groups|group-avatars|events|eventphotos|marketplace|posts|stories|highlights|messages)/[0-9a-f]{32}\.(jpg|jpeg|png|gif|webp|mp4|webm|mov)$",
        RegexOptions.Compiled);

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly IUploadAccessService _access;
    private readonly IWebHostEnvironment _environment;

    public UploadsController(IUploadAccessService access, IWebHostEnvironment environment)
    {
        _access = access;
        _environment = environment;
    }

    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(string? path)
    {
        var match = FilePattern.Match(path ?? string.Empty);
        if (!match.Success)
            return NotFound();

        var folder = match.Groups["folder"].Value;
        var physicalPath = Path.Combine(_environment.WebRootPath, "uploads", folder, Path.GetFileName(path!));
        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        if (!await _access.CanViewAsync(User.GetUserId(), folder, "/uploads/" + path))
            return NotFound();

        if (!ContentTypes.TryGetContentType(physicalPath, out var contentType))
            contentType = "application/octet-stream";

        // Kept by the browser for a few minutes, for this person only; videos can be played and skipped through (ranges).
        Response.Headers.CacheControl = "private, max-age=300";
        Response.Headers.Vary = "Cookie";
        return PhysicalFile(physicalPath, contentType, enableRangeProcessing: true);
    }
}
