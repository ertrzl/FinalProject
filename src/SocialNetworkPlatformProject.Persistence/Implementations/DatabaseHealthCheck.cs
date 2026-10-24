using Microsoft.Extensions.Diagnostics.HealthChecks;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Implementations;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database cannot be reached.");
    }
}
