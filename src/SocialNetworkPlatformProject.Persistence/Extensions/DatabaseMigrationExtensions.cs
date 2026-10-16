using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Extensions;

// Creates or updates the database tables when the app starts, so a brand-new database (for example the one in the Docker
// container) is ready to use without running "dotnet ef database update" by hand. It only runs when
// "Database:MigrateOnStart" is true, because on a developer's machine or a managed server the schema is usually updated
// on purpose and not as a side effect of starting the app.
// Like DatabaseHealthCheck this is start-up infrastructure, not an application service, so it may use the context directly.
public static class DatabaseMigrationExtensions
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(3);

    // Call once after the app is built and before it starts answering requests.
    public static async Task MigrateDatabaseIfRequestedAsync(this IHost app)
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("Database:MigrateOnStart", false))
            return;

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
        var cancellationToken = app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        // SQL Server in a container can still be starting when the app is already up (the container being "started" does
        // not mean the database accepts connections), so a failed attempt is retried a few times before giving up.
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
                // The last attempt (or a stop request) is not swallowed: starting an app on a database that is not ready
                // would only fail later in a harder to understand way, so the start fails here with the real reason.
                logger.LogCritical(exception, "Updating the database failed after {Attempt} attempts; the application cannot start.", attempt);
                throw;
            }
        }
    }
}
