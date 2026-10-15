using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The person's own open sessions (settings page): see where the account is signed in and end any of them from here.
// `currentSessionId` is the session of the request ("sid" claim of the token; null for a token that has none).
public interface ISessionService
{
    // Newest activity first.
    Task<List<GetSessionDto>> GetMineAsync(Guid currentUserId, Guid? currentSessionId);

    // Ends one session: its refresh token stops working and its access token, photo cookie and live connections are cut off.
    Task EndAsync(Guid currentUserId, Guid sessionId);

    // Ends every session except the current one; returns how many were ended.
    Task<int> EndOthersAsync(Guid currentUserId, Guid? currentSessionId);
}
