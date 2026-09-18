namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// Returned by the Auth endpoints (login/register) — the JWT the frontend stores and sends as Bearer token.
public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
