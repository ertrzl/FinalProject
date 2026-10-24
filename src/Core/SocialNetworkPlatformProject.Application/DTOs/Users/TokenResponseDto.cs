using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Application.DTOs.Users;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public bool IsAdmin { get; set; }

    [JsonIgnore]
    public string? MediaToken { get; set; }

    public DateTime? MediaExpiresAt { get; set; }
}
