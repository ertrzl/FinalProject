namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventMaintenanceService
{
    Task<int> SendDueRemindersAsync(DateTime nowUtc);

    Task<int> PromoteWaitingListsAsync(DateTime nowUtc);
}
