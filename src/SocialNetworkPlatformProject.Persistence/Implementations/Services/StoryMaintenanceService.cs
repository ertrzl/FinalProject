using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class StoryMaintenanceService : IStoryMaintenanceService
{
    // Removed in slices so a big backlog (the server was off for a week, say) never becomes one huge statement;
    // whatever is left after MaxBatchesPerRun is picked up on the next round.
    private const int BatchSize = 200;
    private const int MaxBatchesPerRun = 20;

    private readonly IStoryRepository _stories;
    private readonly IFileStorageService _files;
    private readonly ILogger<StoryMaintenanceService> _logger;

    public StoryMaintenanceService(IStoryRepository stories, IFileStorageService files, ILogger<StoryMaintenanceService> logger)
    {
        _stories = stories;
        _files = files;
        _logger = logger;
    }

    public async Task<int> DeleteExpiredStoriesAsync(DateTime nowUtc)
    {
        var removed = 0;

        for (var batch = 0; batch < MaxBatchesPerRun; batch++)
        {
            var expired = await _stories.GetExpiredAsync(nowUtc, BatchSize);
            if (expired.Count == 0)
                break;

            // The rows go first: once they are gone nobody can see the story, even if a file refuses to be deleted.
            removed += await _stories.DeleteByIdsAsync(expired.Select(s => s.Id).ToList());

            foreach (var story in expired)
            {
                try
                {
                    _files.Delete(story.MediaUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "The file of expired story {StoryId} could not be deleted.", story.Id);
                }
            }

            if (expired.Count < BatchSize)
                break;
        }

        return removed;
    }
}
