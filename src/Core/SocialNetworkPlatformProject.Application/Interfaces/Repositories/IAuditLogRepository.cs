using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(List<AuditLog> Items, int TotalCount)> SearchAsync(string? action, string? term, int page, int pageSize);

    Task<int> DeleteOlderThanAsync(DateTime limitUtc);
}
