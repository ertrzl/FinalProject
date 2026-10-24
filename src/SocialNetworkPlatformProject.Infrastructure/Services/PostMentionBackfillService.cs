using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

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
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var scope = _scopes.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<IPostMentionService>().BackfillAsync();
            if (stored > 0)
                _logger.LogInformation("Post tags: {Count} tag(s) of older posts stored.", stored);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The tags of older posts could not be stored; the \"Etiketlendiklerim\" tab will only show posts written from now on.");
        }
    }
}
