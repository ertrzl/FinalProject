using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Music;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MusicController : ControllerBase
{
    private readonly IMusicSearchService _music;

    public MusicController(IMusicSearchService music)
    {
        _music = music;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<GetMusicTrackDto>>> Search([FromQuery] string? q)
    {
        var query = q?.Trim() ?? string.Empty;
        if (query.Length < 2 || query.Length > 80)
            throw new BadRequestException(Loc.T("music.queryLength"));

        return await _music.SearchAsync(query);
    }
}
