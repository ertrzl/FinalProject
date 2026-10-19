using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// Once, when the application starts: fills the table of "@" tags (PostMention) from the text of the posts written before the table
// existed, so the profile's "Etiketlendiklerim" tab finds old tags too. It does nothing when the table already has rows. Runs in the
// background so a start is never held up by it, and a failure (the table not created yet, say) is only logged.
public class PostMentionBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PostMentionBackfillService> _logger;

    public PostMentionBackfillService(IServiceScopeFactory scopes, ILogger<PostMentionBackfillService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A moment after the start, so the database is up (see the Docker setup) before the first query.
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var scope = _scopes.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<IPostMentionService>().BackfillAsync();
            if (stored > 0)
                _logger.LogInformation("Post tags: {Count} tag(s) of older posts stored.", stored);
        }
        catch (OperationCanceledException)
        {
            // The application is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The tags of older posts could not be stored; the \"Etiketlendiklerim\" tab will only show posts written from now on.");
        }
    }
}
