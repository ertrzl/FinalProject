using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventQueryService : IEventQueryService
{
    // The counts, "am I going?" and "is it full?" are all computed from the Attendees collection.
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private const int MaxPageSize = 50;

    // "Etkinliklerim" shows the most recent N; there is no paging UI for it.
    private const int MaxMineEvents = 100;

    // The attendee tab lists at most this many people; the counts on the event itself stay exact.
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
            // Names only: Enum.TryParse would also accept "2" or "99".
            if (!Enum.GetNames<EventAttendeeStatus>().Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new BadRequestException("Status must be 'Going', 'Interested' or 'Waitlisted'.");
            wanted = Enum.Parse<EventAttendeeStatus>(status, ignoreCase: true);
        }

        var found = await _access.GetViewableAsync(currentUserId, eventId);

        var attendees = await _attendees.GetAll(
                filter: a => a.EventId == eventId && (wanted == null || a.Status == wanted),
                orderBy: a => a.CreatedAt,
                asNoTracking: true)
            .ToListAsync();

        // Going first, then interested, then the waiting list; within each group, whoever signed up first
        // (the waiting list is in queue order).
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

        // Everything that hasn't finished yet (so ongoing events stay listed), limited to what this viewer may see.
        var events = _events.GetAll(filter: e => e.EndsAt > now, asNoTracking: true, includes: AttendeeIncludes)
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

        // An explicit "to" and a quick range can both be given: the earlier limit wins.
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

        // The Id tie-breaker keeps paging stable when several events start at the same minute.
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
            throw new ForbiddenException("Grubun etkinliklerini görmek için üye olmalısın.");

        var now = DateTime.UtcNow;

        var upcoming = await _events.GetAll(
                filter: e => e.GroupId == groupId && e.EndsAt > now,
                orderBy: e => e.StartsAt,
                asNoTracking: true,
                includes: AttendeeIncludes)
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
            _ => throw new BadRequestException("Scope must be 'created', 'past' or 'attending'.")
        };

        var query = _events.GetAll(filter: filter, asNoTracking: true, includes: AttendeeIncludes)
            .Where(await _access.GetVisibilityFilterAsync(currentUserId));

        // What's coming is listed soonest first; what already happened (or I organized) newest first.
        query = scope == EventMineScope.Attending
            ? query.OrderBy(e => e.StartsAt)
            : query.OrderByDescending(e => e.StartsAt);

        var mine = await query.Take(MaxMineEvents).ToListAsync();

        return await _builder.BuildManyAsync(mine, currentUserId);
    }
}
