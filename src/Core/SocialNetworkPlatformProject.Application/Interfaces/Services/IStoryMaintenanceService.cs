namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Housekeeping for stories, run on a timer by the host (see IEventMaintenanceService for why each call needs its own scope).
public interface IStoryMaintenanceService
{
    // Deletes the stories that have expired: their rows and their image/video files. Returns how many were removed.
    Task<int> DeleteExpiredStoriesAsync(DateTime nowUtc);
}
