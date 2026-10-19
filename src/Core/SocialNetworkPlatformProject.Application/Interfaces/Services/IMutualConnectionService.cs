using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// "Ortak arkadaşlar" and "ortak gruplar" between the viewer and the owner of a profile.
public interface IMutualConnectionService
{
    // Friends both people have and groups both belong to. Somebody who blocked the viewer looks as if they do not exist; people blocked
    // either way are left out of the friends. Groups: only the ones the viewer belongs to themselves, so a private group is never
    // shown to somebody who is not in it. Your own profile has nothing "mutual": empty.
    Task<GetMutualConnectionsDto> GetAsync(Guid viewerId, Guid userId);
}
