using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Extensions;

public static class DatabaseMigrationExtensions
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(3);

    public static async Task MigrateDatabaseIfRequestedAsync(this IHost app)
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("Database:MigrateOnStart", false))
            return;

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
        var cancellationToken = app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("The database is up to date (attempt {Attempt}).", attempt);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Updating the database failed (attempt {Attempt} of {MaxAttempts}); trying again in {Seconds} seconds.",
                    attempt, MaxAttempts, DelayBetweenAttempts.TotalSeconds);
                await Task.Delay(DelayBetweenAttempts, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Updating the database failed after {Attempt} attempts; the application cannot start.", attempt);
                throw;
            }
        }
    }
}
