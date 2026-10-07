using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventDtoBuilder : IEventDtoBuilder
{
    private readonly IMapper _mapper;
    private readonly IUserRepository _users;
    private readonly IGroupRepository _groups;
    private readonly IEventInviteRepository _invites;
    private readonly IEventAttendeeRepository _attendees;

    public EventDtoBuilder(
        IMapper mapper,
        IUserRepository users,
        IGroupRepository groups,
        IEventInviteRepository invites,
        IEventAttendeeRepository attendees)
    {
        _mapper = mapper;
        _users = users;
        _groups = groups;
        _invites = invites;
        _attendees = attendees;
    }

    public GetEventDto Build(Event source, Guid viewerId)
    {
        return Fill(source, Summarize(source, viewerId), viewerId);
    }

    public async Task<List<GetEventDto>> BuildManyAsync(IEnumerable<Event> events, Guid viewerId)
    {
        var list = events.ToList();

        // Who is coming is counted by the database for the whole page at once; the attendee rows stay where they are.
        var attendance = await _attendees.GetAttendanceAsync(list.Select(e => e.Id).ToList(), viewerId);
        var dtos = list.Select(e => Fill(e, attendance.GetValueOrDefault(e.Id, EventAttendance.None), viewerId)).ToList();

        var groupIds = dtos.Where(d => d.GroupId.HasValue).Select(d => d.GroupId!.Value).Distinct().ToList();
        if (groupIds.Count > 0)
        {
            var names = await _groups.GetAll(g => groupIds.Contains(g.Id), asNoTracking: true)
                .ToDictionaryAsync(g => g.Id, g => g.Name);
            foreach (var dto in dtos.Where(d => d.GroupId.HasValue))
                dto.GroupName = names.GetValueOrDefault(dto.GroupId!.Value);
        }

        return dtos;
    }

    public async Task<GetEventDto> BuildDetailAsync(Event source, Guid viewerId)
    {
        var dto = Build(source, viewerId);

        var organizer = await _users.GetSummaryAsync(source.CreatedByUserId);
        dto.CreatedByName = organizer?.FullName;
        dto.CreatedByAvatarUrl = organizer?.AvatarUrl;

        if (source.GroupId.HasValue)
            dto.GroupName = (await _groups.GetByIdAsync(source.GroupId.Value))?.Name;

        // A pending invite to this event: lets the page show "X seni davet etti" with accept / decline.
        if (!dto.IsOwner && dto.CurrentUserStatus == "None")
        {
            var inviterId = await _invites
                .GetAll(i => i.EventId == source.Id && i.InvitedUserId == viewerId, asNoTracking: true)
                .Select(i => (Guid?)i.InvitedByUserId)
                .FirstOrDefaultAsync();
            if (inviterId.HasValue)
                dto.InvitedByName = (await _users.GetSummaryAsync(inviterId.Value))?.FullName;
        }

        return dto;
    }

    // The same numbers BuildMany gets from the database, read off an event whose Attendees are loaded.
    private static EventAttendance Summarize(Event source, Guid viewerId)
    {
        var mine = source.Attendees.FirstOrDefault(a => a.UserId == viewerId);
        var waitlist = source.Attendees.Where(a => a.Status == EventAttendeeStatus.Waitlisted).OrderBy(a => a.WaitlistedAt).ToList();

        return new EventAttendance(
            source.Attendees.Count(a => a.Status == EventAttendeeStatus.Going),
            source.Attendees.Count(a => a.Status == EventAttendeeStatus.Interested),
            waitlist.Count,
            mine?.Status,
            mine?.Status == EventAttendeeStatus.Waitlisted ? waitlist.FindIndex(a => a.UserId == viewerId) + 1 : 0);
    }

    private GetEventDto Fill(Event source, EventAttendance attendance, Guid viewerId)
    {
        var dto = _mapper.Map<GetEventDto>(source);
        dto.GoingCount = attendance.Going;
        dto.InterestedCount = attendance.Interested;
        dto.CurrentUserStatus = attendance.MyStatus?.ToString() ?? "None";
        dto.IsOwner = source.CreatedByUserId == viewerId;

        dto.IsFull = source.IsFullWith(attendance.Going);
        dto.SpotsLeft = source.Capacity.HasValue ? Math.Max(source.Capacity.Value - attendance.Going, 0) : null;

        dto.WaitlistCount = attendance.Waitlisted;
        if (attendance.MyStatus == EventAttendeeStatus.Waitlisted)
            dto.MyWaitlistPosition = attendance.MyWaitlistPosition;

        // The meeting link is for the people who are actually attending, not for everyone browsing events.
        if (source.IsOnline && (dto.IsOwner || attendance.MyStatus == EventAttendeeStatus.Going))
            dto.OnlineLink = source.OnlineLink;

        return dto;
    }
}
