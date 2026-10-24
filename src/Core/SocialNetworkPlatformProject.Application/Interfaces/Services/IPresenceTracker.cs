namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPresenceTracker
{
    bool IsOnline(Guid userId);
}
