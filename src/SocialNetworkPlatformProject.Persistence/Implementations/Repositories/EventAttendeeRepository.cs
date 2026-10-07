using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class EventAttendeeRepository : Repository<EventAttendee>, IEventAttendeeRepository
{
    public EventAttendeeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, EventAttendance>> GetAttendanceAsync(IReadOnlyCollection<Guid> eventIds, Guid viewerId)
    {
        var ids = eventIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, EventAttendance>();

        var counts = await _dbSet.AsNoTracking()
            .Where(a => ids.Contains(a.EventId))
            .GroupBy(a => new { a.EventId, a.Status })
            .Select(g => new { g.Key.EventId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var mine = await _dbSet.AsNoTracking()
            .Where(a => ids.Contains(a.EventId) && a.UserId == viewerId)
            .Select(a => new { a.EventId, a.Status })
            .ToDictionaryAsync(a => a.EventId, a => a.Status);

        // Where the viewer stands in a waiting line = how many people queued before them, plus one.
        var waitingIds = mine.Where(m => m.Value == EventAttendeeStatus.Waitlisted).Select(m => m.Key).ToList();
        var ahead = waitingIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _dbSet.AsNoTracking()
                .Where(a => waitingIds.Contains(a.EventId) && a.Status == EventAttendeeStatus.Waitlisted
                    && _dbSet.Any(me => me.EventId == a.EventId && me.UserId == viewerId && me.WaitlistedAt > a.WaitlistedAt))
                .GroupBy(a => a.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);

        int CountOf(Guid eventId, EventAttendeeStatus status) =>
            counts.Where(c => c.EventId == eventId && c.Status == status).Select(c => c.Count).FirstOrDefault();

        return ids.ToDictionary(id => id, id =>
        {
            EventAttendeeStatus? myStatus = mine.TryGetValue(id, out var status) ? status : null;

            return new EventAttendance(
                CountOf(id, EventAttendeeStatus.Going),
                CountOf(id, EventAttendeeStatus.Interested),
                CountOf(id, EventAttendeeStatus.Waitlisted),
                myStatus,
                myStatus == EventAttendeeStatus.Waitlisted ? ahead.GetValueOrDefault(id) + 1 : 0);
        });
    }
}
