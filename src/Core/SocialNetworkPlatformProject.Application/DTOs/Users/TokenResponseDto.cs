using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// Returned by the Auth endpoints (login/register) — the JWT the frontend stores and sends as Bearer token.
public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Shows the admin panel link; the server checks the role on every admin request anyway.
    public bool IsAdmin { get; set; }

    // The photo cookie (MediaCookie) that came with this answer: the controller filter puts the token into a cookie and the
    // page only learns until when it is good, so a session without one can ask for it (POST api/auth/media-session).
    [JsonIgnore]
    public string? MediaToken { get; set; }

    public DateTime? MediaExpiresAt { get; set; }
}
