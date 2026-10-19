using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventCheckInService : IEventCheckInService
{
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventAccessService _access;
    private readonly IUserRepository _users;

    public EventCheckInService(IEventAttendeeRepository attendees, IEventAccessService access, IUserRepository users)
    {
        _attendees = attendees;
        _access = access;
        _users = users;
    }

    public async Task<GetEventAttendanceDto> GetAsync(Guid currentUserId, Guid eventId)
    {
        var ev = await _access.GetViewableAsync(currentUserId, eventId);
        EnsureOrganizer(ev, currentUserId);

        return await BuildAsync(ev);
    }

    public async Task<GetEventAttendanceDto> SetAsync(Guid currentUserId, Guid eventId, Guid userId, PutEventCheckInDto dto)
    {
        var ev = await _access.GetViewableAsync(currentUserId, eventId);
        EnsureOrganizer(ev, currentUserId);

        var now = DateTime.UtcNow;
        if (now < ev.StartsAt)
            throw new BadRequestException(Loc.T("eventCheckIn.notOpen"));
        if (now > ev.EndsAt + EventLimits.CheckInGrace)
            throw new BadRequestException(Loc.T("eventCheckIn.closed", EventLimits.CheckInGrace.Days));

        var attendee = await _attendees.GetAll(a => a.EventId == eventId && a.UserId == userId).FirstOrDefaultAsync();
        if (attendee == null || attendee.Status != EventAttendeeStatus.Going)
            throw new BadRequestException(Loc.T("eventCheckIn.notGoing"));

        if (dto.CheckedIn)
        {
            // Ticking someone who is already ticked keeps the first time (and who gave it).
            if (attendee.CheckedInAt == null)
            {
                attendee.CheckedInAt = now;
                attendee.CheckedInByUserId = currentUserId;
            }
        }
        else
        {
            attendee.CheckedInAt = null;
            attendee.CheckedInByUserId = null;
        }

        await _attendees.SaveChangesAsync();

        return await BuildAsync(ev);
    }

    // The organizer runs the list. (There are no co-hosts: if they are added, this is the one place that decides.)
    private static void EnsureOrganizer(Event ev, Guid userId)
    {
        if (ev.CreatedByUserId != userId)
            throw new ForbiddenException(Loc.T("eventCheckIn.organizerOnly"));
    }

    private async Task<GetEventAttendanceDto> BuildAsync(Event ev)
    {
        var going = await _attendees.GetAll(
                a => a.EventId == ev.Id && a.Status == EventAttendeeStatus.Going, asNoTracking: true)
            .ToListAsync();

        var people = await _users.GetSummariesAsync(going.Select(a => a.UserId));
        var now = DateTime.UtcNow;

        var rows = going
            .Where(a => people.ContainsKey(a.UserId))
            .Select(a => new GetEventCheckInRowDto
            {
                UserId = a.UserId,
                FullName = people[a.UserId].FullName,
                AvatarUrl = people[a.UserId].AvatarUrl,
                CheckedIn = a.CheckedInAt != null,
                CheckedInAt = a.CheckedInAt
            })
            .OrderBy(r => r.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var closesAt = ev.EndsAt + EventLimits.CheckInGrace;
        return new GetEventAttendanceDto
        {
            IsOpen = now >= ev.StartsAt && now <= closesAt,
            State = now < ev.StartsAt ? "NotYet" : now <= closesAt ? "Open" : "Closed",
            OpensAt = ev.StartsAt,
            ClosesAt = closesAt,
            GoingCount = rows.Count,
            CheckedInCount = rows.Count(r => r.CheckedIn),
            Items = rows
        };
    }
}
