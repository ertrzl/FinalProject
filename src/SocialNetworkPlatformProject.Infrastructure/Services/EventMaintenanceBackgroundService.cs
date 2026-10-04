using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// Wakes up on a timer and asks IEventMaintenanceService to do its housekeeping (event reminders, waiting lists).
// The interval comes from "Events:MaintenanceIntervalSeconds" (default 60).
public class EventMaintenanceBackgroundService : BackgroundService
{
    private const int DefaultIntervalSeconds = 60;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EventMaintenanceBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public EventMaintenanceBackgroundService(
        IServiceScopeFactory scopes,
        ILogger<EventMaintenanceBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(configuration.GetValue("Events:MaintenanceIntervalSeconds", DefaultIntervalSeconds), 1));
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
        var reminders = await RunStepAsync("reminders", maintenance => maintenance.SendDueRemindersAsync(now), stoppingToken);
        var promoted = await RunStepAsync("waiting lists", maintenance => maintenance.PromoteWaitingListsAsync(now), stoppingToken);

        if (reminders > 0 || promoted > 0)
        {
            _logger.LogInformation("Event maintenance: {Reminders} reminder(s) sent, {Promoted} person(s) moved up from a waiting list.",
                reminders, promoted);
        }
    }

    private async Task<int> RunStepAsync(string name, Func<IEventMaintenanceService, Task<int>> step, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            return await step(scope.ServiceProvider.GetRequiredService<IEventMaintenanceService>());
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // One bad step (a database hiccup, say) must not kill the timer: log it and try again next round.
            _logger.LogError(ex, "Event maintenance step '{Step}' failed.", name);
            return 0;
        }
    }
}
