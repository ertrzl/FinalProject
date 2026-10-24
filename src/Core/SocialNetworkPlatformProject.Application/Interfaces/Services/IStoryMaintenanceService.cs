namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IStoryMaintenanceService
{
    Task<int> DeleteExpiredStoriesAsync(DateTime nowUtc);
}
