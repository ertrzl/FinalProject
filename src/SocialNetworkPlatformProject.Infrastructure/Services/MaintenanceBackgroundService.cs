using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// Wakes up on a timer and does the housekeeping nobody triggers with a request: event reminders, waiting lists and
// the clean-up of expired stories. The interval comes from "Maintenance:IntervalSeconds" (default 60).
public class MaintenanceBackgroundService : BackgroundService
{
    private const int DefaultIntervalSeconds = 60;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MaintenanceBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public MaintenanceBackgroundService(
        IServiceScopeFactory scopes,
        ILogger<MaintenanceBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(configuration.GetValue("Maintenance:IntervalSeconds", DefaultIntervalSeconds), 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // The application is shutting down.
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        // Each step gets its own scope and DbContext, so one failing step can't leave stale state for the next.
        var reminders = await RunStepAsync<IEventMaintenanceService>("event reminders", m => m.SendDueRemindersAsync(now), stoppingToken);
        var promoted = await RunStepAsync<IEventMaintenanceService>("waiting lists", m => m.PromoteWaitingListsAsync(now), stoppingToken);
        var stories = await RunStepAsync<IStoryMaintenanceService>("expired stories", m => m.DeleteExpiredStoriesAsync(now), stoppingToken);

        if (reminders > 0 || promoted > 0)
        {
            _logger.LogInformation("Event maintenance: {Reminders} reminder(s) sent, {Promoted} person(s) moved up from a waiting list.",
                reminders, promoted);
        }

        if (stories > 0)
            _logger.LogInformation("Story clean-up: {Stories} expired stor(ies) removed.", stories);
    }

    private async Task<int> RunStepAsync<TService>(string name, Func<TService, Task<int>> step, CancellationToken stoppingToken)
        where TService : notnull
    {
        try
        {
            using var scope = _scopes.CreateScope();
            return await step(scope.ServiceProvider.GetRequiredService<TService>());
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // One bad step (a database hiccup, say) must not kill the timer: log it and try again next round.
            _logger.LogError(ex, "Maintenance step '{Step}' failed.", name);
            return 0;
        }
    }
}
