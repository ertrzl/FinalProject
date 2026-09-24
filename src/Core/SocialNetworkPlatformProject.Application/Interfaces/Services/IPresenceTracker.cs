namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// "Online" means the user has at least one live SignalR connection right now (tracked in Infrastructure).
public interface IPresenceTracker
{
    bool IsOnline(Guid userId);
}
