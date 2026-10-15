namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Housekeeping for rows that only pile up: sign-in tokens that ran out, old notifications, hashtags no post uses any more,
// old admin log lines. Run on a timer by the host (see IEventMaintenanceService for why each call needs its own scope).
public interface IDataCleanupService
{
    // Returns how many rows were removed in all.
    Task<int> CleanUpAsync(DateTime nowUtc);
}
