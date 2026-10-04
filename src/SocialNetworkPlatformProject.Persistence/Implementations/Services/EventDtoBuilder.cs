using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

    public EventDtoBuilder(IMapper mapper, IUserRepository users, IGroupRepository groups, IEventInviteRepository invites)
    {
        _mapper = mapper;
        _users = users;
        _groups = groups;
        _invites = invites;
    }

    public GetEventDto Build(Event source, Guid viewerId)
    {
        var dto = _mapper.Map<GetEventDto>(source);
        var mine = source.Attendees.FirstOrDefault(a => a.UserId == viewerId);
        dto.CurrentUserStatus = mine?.Status.ToString() ?? "None";
        dto.IsOwner = source.CreatedByUserId == viewerId;

        dto.IsFull = source.IsFull;
        dto.SpotsLeft = source.Capacity.HasValue ? Math.Max(source.Capacity.Value - source.GoingCount, 0) : null;

        var waitlist = source.Attendees.Where(a => a.Status == EventAttendeeStatus.Waitlisted).OrderBy(a => a.WaitlistedAt).ToList();
        dto.WaitlistCount = waitlist.Count;
        if (mine?.Status == EventAttendeeStatus.Waitlisted)
            dto.MyWaitlistPosition = waitlist.FindIndex(a => a.UserId == viewerId) + 1;

        // The meeting link is for the people who are actually attending, not for everyone browsing events.
        if (source.IsOnline && (dto.IsOwner || mine?.Status == EventAttendeeStatus.Going))
            dto.OnlineLink = source.OnlineLink;

        return dto;
    }

    public async Task<List<GetEventDto>> BuildManyAsync(IEnumerable<Event> events, Guid viewerId)
    {
        var dtos = events.Select(e => Build(e, viewerId)).ToList();

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
}
