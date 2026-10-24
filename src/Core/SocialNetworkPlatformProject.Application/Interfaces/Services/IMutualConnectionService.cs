using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMutualConnectionService
{
    Task<GetMutualConnectionsDto> GetAsync(Guid viewerId, Guid userId);
}
