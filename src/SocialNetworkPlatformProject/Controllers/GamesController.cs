using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games)
    {
        _games = games;
    }

    [HttpGet]
    public async Task<ActionResult<GetGamesOverviewDto>> GetOverview()
    {
        return await _games.GetOverviewAsync(User.GetUserId());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetGameDto>> Get(Guid id)
    {
        return await _games.GetAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetGameDto>> Invite(PostGameDto dto)
    {
        return await _games.InviteAsync(User.GetUserId(), dto);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<GetGameDto>> Accept(Guid id)
    {
        return await _games.AcceptAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<ActionResult<GetGameDto>> Decline(Guid id)
    {
        return await _games.DeclineAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<GetGameDto>> Cancel(Guid id)
    {
        return await _games.CancelAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/resign")]
    public async Task<ActionResult<GetGameDto>> Resign(Guid id)
    {
        return await _games.ResignAsync(User.GetUserId(), id);
    }

    [HttpGet("random-fleet")]
    public ActionResult<GetRandomFleetDto> RandomFleet()
    {
        return _games.GetRandomFleet();
    }

    [HttpPost("{id:guid}/place")]
    public async Task<ActionResult<GetGameDto>> Place(Guid id, PostGamePlaceDto dto)
    {
        return await _games.PlaceAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/shoot")]
    public async Task<ActionResult<GetGameDto>> Shoot(Guid id, PostGameShootDto dto)
    {
        return await _games.ShootAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/choose")]
    public async Task<ActionResult<GetGameDto>> Choose(Guid id, PostGameChoiceDto dto)
    {
        return await _games.ChooseAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/move")]
    public async Task<ActionResult<GetGameDto>> Move(Guid id, PostGameMoveDto dto)
    {
        return await _games.MoveAsync(User.GetUserId(), id, dto);
    }
}
