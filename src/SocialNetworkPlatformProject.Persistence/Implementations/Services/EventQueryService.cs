using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventQueryService : IEventQueryService
{
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private const int MaxPageSize = 50;

    private const int MaxMineEvents = 100;

    private const int MaxAttendeesListed = 200;

    private readonly IEventRepository _events;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventAccessService _access;
    private readonly IEventDtoBuilder _builder;
    private readonly IUserRepository _users;

    public EventQueryService(
        IEventRepository events,
        IEventAttendeeRepository attendees,
        IEventAccessService access,
        IEventDtoBuilder builder,
        IUserRepository users)
    {
        _events = events;
        _attendees = attendees;
        _access = access;
        _builder = builder;
        _users = users;
    }

    public async Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);
        return await _builder.BuildDetailAsync(found, currentUserId);
    }

    public async Task<List<GetEventAttendeeDto>> GetAttendeesAsync(Guid currentUserId, Guid eventId, string? status)
    {
        EventAttendeeStatus? wanted = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.GetNames<EventAttendeeStatus>().Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new BadRequestException(Loc.T("eventQuery.status"));
            wanted = Enum.Parse<EventAttendeeStatus>(status, ignoreCase: true);
        }

        var found = await _access.GetViewableAsync(currentUserId, eventId);

        var attendees = await _attendees.GetAll(
                filter: a => a.EventId == eventId && (wanted == null || a.Status == wanted),
                orderBy: a => a.CreatedAt,
                asNoTracking: true)
            .ToListAsync();

        var listed = attendees
            .OrderBy(a => a.Status switch { EventAttendeeStatus.Going => 0, EventAttendeeStatus.Interested => 1, _ => 2 })
            .ThenBy(a => a.WaitlistedAt ?? a.CreatedAt)
            .Take(MaxAttendeesListed)
            .ToList();

        var people = await _users.GetSummariesAsync(listed.Select(a => a.UserId));

        return listed
            .Where(a => people.ContainsKey(a.UserId))
            .Select(a => new GetEventAttendeeDto
            {
                UserId = a.UserId,
                FullName = people[a.UserId].FullName,
                AvatarUrl = people[a.UserId].AvatarUrl,
                Status = a.Status.ToString(),
                IsOrganizer = a.UserId == found.CreatedByUserId
            })
            .ToList();
    }

    public async Task<PagedResult<GetEventDto>> GetUpcomingAsync(Guid currentUserId, EventQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var now = DateTime.UtcNow;

        var events = _events.GetAll(filter: e => e.EndsAt > now, asNoTracking: true)
            .Where(await _access.GetVisibilityFilterAsync(currentUserId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            events = events.Where(e => e.Title.Contains(term)
                || (e.Description != null && e.Description.Contains(term))
                || (e.Location != null && e.Location.Contains(term)));
        }

        if (query.IsOnline.HasValue)
            events = events.Where(e => e.IsOnline == query.IsOnline.Value);

        if (query.From.HasValue)
        {
            var from = query.From.Value.ToUtc();
            events = events.Where(e => e.EndsAt > from);
        }

        DateTime? latestStart = query.To?.ToUtc();
        if (!string.IsNullOrEmpty(query.When))
        {
            EventDateRange.TryResolveZone(query.TimeZone, out var zone);
            var rangeEnd = EventDateRange.EndUtc(query.When, now, zone);
            latestStart = latestStart.HasValue && latestStart.Value < rangeEnd ? latestStart : rangeEnd;
        }

        if (latestStart.HasValue)
        {
            var to = latestStart.Value;
            events = events.Where(e => e.StartsAt < to);
        }

        var total = await events.CountAsync();

        var items = await events
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetEventDto>
        {
            Items = await _builder.BuildManyAsync(items, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<List<GetEventDto>> GetGroupEventsAsync(Guid currentUserId, Guid groupId)
    {
        if (!await _access.IsGroupMemberAsync(groupId, currentUserId))
            throw new ForbiddenException(Loc.T("eventQuery.groupMembersOnly"));

        var now = DateTime.UtcNow;

        var upcoming = await _events.GetAll(
                filter: e => e.GroupId == groupId && e.EndsAt > now,
                orderBy: e => e.StartsAt,
                asNoTracking: true)
            .ToListAsync();

        return await _builder.BuildManyAsync(upcoming, currentUserId);
    }

    public async Task<List<GetEventDto>> GetMineAsync(Guid currentUserId, string scope)
    {
        var now = DateTime.UtcNow;

        Expression<Func<Event, bool>> filter = scope switch
        {
            EventMineScope.Created => e => e.CreatedByUserId == currentUserId,
            EventMineScope.Past => e => e.EndsAt <= now && e.Attendees.Any(a => a.UserId == currentUserId),
            EventMineScope.Attending => e => e.EndsAt > now && e.Attendees.Any(a => a.UserId == currentUserId),
            _ => throw new BadRequestException(Loc.T("eventQuery.scope"))
        };

        var query = _events.GetAll(filter: filter, asNoTracking: true)
            .Where(await _access.GetVisibilityFilterAsync(currentUserId));

        query = scope == EventMineScope.Attending
            ? query.OrderBy(e => e.StartsAt)
            : query.OrderByDescending(e => e.StartsAt);

        var mine = await query.Take(MaxMineEvents).ToListAsync();

        return await _builder.BuildManyAsync(mine, currentUserId);
    }
}
