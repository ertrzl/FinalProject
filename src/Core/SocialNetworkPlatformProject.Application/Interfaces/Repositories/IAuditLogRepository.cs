using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    // Newest first. `action` is one line ("user.suspend") or a whole kind ("user" = every "user.*" line); `term` looks in the
    // admin's name and the details.
    Task<(List<AuditLog> Items, int TotalCount)> SearchAsync(string? action, string? term, int page, int pageSize);

    // Removes the lines older than the given time; returns how many went.
    Task<int> DeleteOlderThanAsync(DateTime limitUtc);
}
