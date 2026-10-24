namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IDataCleanupService
{
    Task<int> CleanUpAsync(DateTime nowUtc);
}
