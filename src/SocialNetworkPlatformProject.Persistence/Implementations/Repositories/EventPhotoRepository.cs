using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class EventPhotoRepository : Repository<EventPhoto>, IEventPhotoRepository
{
    public EventPhotoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<List<EventPhoto>> GetByEventAsync(Guid eventId)
    {
        return _dbSet.AsNoTracking()
            .Where(p => p.EventId == eventId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    public Task<int> CountByEventAsync(Guid eventId)
    {
        return _dbSet.CountAsync(p => p.EventId == eventId);
    }

    public Task<int> CountByUploaderAsync(Guid eventId, Guid userId)
    {
        return _dbSet.CountAsync(p => p.EventId == eventId && p.UploadedByUserId == userId);
    }

    public Task<Guid?> FindEventIdByUrlAsync(string url)
    {
        return _dbSet.AsNoTracking()
            .Where(p => p.Url == url)
            .Select(p => (Guid?)p.EventId)
            .FirstOrDefaultAsync();
    }

    public Task<List<string>> GetUrlsByEventAsync(Guid eventId)
    {
        return _dbSet.AsNoTracking()
            .Where(p => p.EventId == eventId)
            .Select(p => p.Url)
            .ToListAsync();
    }
}
