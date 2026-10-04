namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Housekeeping that nobody triggers with a request: run on a timer by the host. Each method must be called in its
// own scope (its own DbContext): when a save loses a concurrency conflict the context keeps the stale change, so a
// second step in the same scope would trip over it.
public interface IEventMaintenanceService
{
    // Sends the "starts in a day / in an hour" reminders that have become due. Returns how many events were reminded.
    Task<int> SendDueRemindersAsync(DateTime nowUtc);

    // Moves people up from waiting lists that have a free spot, which also repairs spots freed without any request
    // (a deleted account, say). Returns how many people moved up.
    Task<int> PromoteWaitingListsAsync(DateTime nowUtc);
}
