using System.Linq.Expressions;
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
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventInviteRepository _invites;

    public EventAccessService(
        IEventRepository events,
        IGroupRepository groups,
        IGroupMemberRepository members,
        IEventAttendeeRepository attendees,
        IEventInviteRepository invites)
    {
        _events = events;
        _groups = groups;
        _members = members;
        _attendees = attendees;
        _invites = invites;
    }

    public async Task<Event> GetViewableAsync(Guid viewerId, Guid eventId, params string[] includes)
    {
        var found = await _events.GetByIdAsync(eventId, includes)
            ?? throw new NotFoundException("Event not found.");

        // Same answer as "doesn't exist": a group event shouldn't reveal itself to people outside the group.
        if (found.GroupId.HasValue && !await IsGroupMemberAsync(found.GroupId.Value, viewerId))
            throw new NotFoundException("Event not found.");

        if (found.IsPrivate && !await CanSeePrivateEventAsync(found, viewerId))
            throw new NotFoundException("Event not found.");

        return found;
    }

    public async Task<Expression<Func<Event, bool>>> GetVisibilityFilterAsync(Guid viewerId)
    {
        var myGroupIds = await GetMyGroupIdsAsync(viewerId);

        return e => (e.GroupId == null || myGroupIds.Contains(e.GroupId.Value))
            && (!e.IsPrivate
                || e.CreatedByUserId == viewerId
                || e.Attendees.Any(a => a.UserId == viewerId)
                || e.Invites.Any(i => i.InvitedUserId == viewerId));
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

    // Having been invited is enough to see (and accept) a private event, so an invitation never leads to a 404.
    private async Task<bool> CanSeePrivateEventAsync(Event ev, Guid viewerId)
    {
        return ev.CreatedByUserId == viewerId
            || await _attendees.AnyAsync(a => a.EventId == ev.Id && a.UserId == viewerId)
            || await _invites.AnyAsync(i => i.EventId == ev.Id && i.InvitedUserId == viewerId);
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
