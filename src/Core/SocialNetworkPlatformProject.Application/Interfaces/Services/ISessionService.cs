using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ISessionService
{
    Task<List<GetSessionDto>> GetMineAsync(Guid currentUserId, Guid? currentSessionId);

    Task EndAsync(Guid currentUserId, Guid sessionId);

    Task<int> EndOthersAsync(Guid currentUserId, Guid? currentSessionId);
}
