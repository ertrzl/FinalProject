using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Controllers;

// F1: registration + login (index.html / register.html)
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponseDto>> Register([FromForm] RegisterDto dto)
    {
        return await _auth.RegisterAsync(dto);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginDto dto)
    {
        return await _auth.LoginAsync(dto);
    }
}
