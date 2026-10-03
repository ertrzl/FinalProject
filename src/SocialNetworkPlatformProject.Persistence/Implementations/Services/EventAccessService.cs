using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventAccessService : IEventAccessService
{
    private readonly IEventRepository _events;
    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _members;

    public EventAccessService(IEventRepository events, IGroupRepository groups, IGroupMemberRepository members)
    {
        _events = events;
        _groups = groups;
        _members = members;
    }

    public async Task<Event> GetViewableAsync(Guid viewerId, Guid eventId, params string[] includes)
    {
        var found = await _events.GetByIdAsync(eventId, includes)
            ?? throw new NotFoundException("Event not found.");

        // Same answer as "doesn't exist": a group event shouldn't reveal itself to people outside the group.
        if (found.GroupId.HasValue && !await IsGroupMemberAsync(found.GroupId.Value, viewerId))
            throw new NotFoundException("Event not found.");

        return found;
    }

    public async Task<List<Guid>> GetMyGroupIdsAsync(Guid viewerId)
    {
        return await _members.GetAll(m => m.UserId == viewerId, asNoTracking: true)
            .Select(m => m.GroupId)
            .ToListAsync();
    }

    public async Task<List<Guid>> FilterViewersAsync(Event ev, IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (!ev.GroupId.HasValue || ids.Count == 0)
            return ids;

        var groupId = ev.GroupId.Value;
        return await _members.GetAll(m => m.GroupId == groupId && ids.Contains(m.UserId), asNoTracking: true)
            .Select(m => m.UserId)
            .ToListAsync();
    }

    public Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId)
    {
        return _members.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
    }

    public async Task EnsureCanCreateGroupEventAsync(Guid userId, Guid groupId)
    {
        if (!await _groups.AnyAsync(g => g.Id == groupId))
            throw new NotFoundException("Group not found.");

        var membership = await _members.GetAll(m => m.GroupId == groupId && m.UserId == userId, asNoTracking: true)
            .FirstOrDefaultAsync()
            ?? throw new ForbiddenException("Bu grubun üyesi değilsin.");

        if (membership.Role == GroupMemberRole.Member)
            throw new ForbiddenException("Grup etkinliği oluşturmak için yönetici ya da moderatör olmalısın.");
    }
}
