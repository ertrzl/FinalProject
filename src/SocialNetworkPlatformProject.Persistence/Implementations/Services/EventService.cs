using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventService : IEventService
{
    // GetEventDto's Going/Interested counts are computed from the Attendees collection.
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    // "Etkinliklerim" shows the most recent N; there is no paging UI for it.
    private const int MaxMineEvents = 100;

    // The attendee tab lists at most this many people; the counts on the event itself stay exact.
    private const int MaxAttendeesListed = 200;

    private const int MaxOnlineLinkLength = 500; // matches the OnlineLink column

    private readonly IEventRepository _events;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventInviteRepository _invites;
    private readonly IGroupRepository _groups;
    private readonly IEventAccessService _access;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public EventService(
        IEventRepository events,
        IEventAttendeeRepository attendees,
        IEventInviteRepository invites,
        IGroupRepository groups,
        IEventAccessService access,
        IFileStorageService files,
        INotificationService notifications,
        IUserRepository users,
        IMapper mapper)
    {
        _events = events;
        _attendees = attendees;
        _invites = invites;
        _groups = groups;
        _access = access;
        _files = files;
        _notifications = notifications;
        _users = users;
        _mapper = mapper;
    }

    public async Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto)
    {
        // Checked before the cover is saved, so a refused request doesn't leave an orphan file behind.
        if (dto.GroupId.HasValue)
            await _access.EnsureCanCreateGroupEventAsync(currentUserId, dto.GroupId.Value);

        string? coverUrl = null;
        if (dto.CoverImage != null)
            coverUrl = await _files.SaveImageAsync(dto.CoverImage, "events");

        var newEvent = new Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location?.Trim(),
            CoverImageUrl = coverUrl,
            StartsAt = dto.StartsAt,
            Capacity = dto.Capacity,
            IsOnline = dto.IsOnline,
            OnlineLink = CleanOnlineLink(dto),
            GroupId = dto.GroupId,
            CreatedByUserId = currentUserId
        };

        await _events.AddAsync(newEvent);
        await _events.SaveChangesAsync();

        return await ToDetailDtoAsync(newEvent, currentUserId);
    }

    public async Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        return await ToDetailDtoAsync(found, currentUserId);
    }

    public async Task<List<GetEventAttendeeDto>> GetAttendeesAsync(Guid currentUserId, Guid eventId, string? status)
    {
        EventAttendeeStatus? wanted = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EventAttendeeStatus>(status, out var parsed))
                throw new BadRequestException("Status must be either 'Going' or 'Interested'.");
            wanted = parsed;
        }

        var found = await _access.GetViewableAsync(currentUserId, eventId);

        var attendees = await _attendees.GetAll(
                filter: a => a.EventId == eventId && (wanted == null || a.Status == wanted),
                orderBy: a => a.CreatedAt,
                asNoTracking: true)
            .ToListAsync();

        // Going first, then interested; within each group, whoever signed up first.
        var listed = attendees
            .OrderBy(a => a.Status == EventAttendeeStatus.Going ? 0 : 1)
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

    public async Task<List<GetEventDto>> GetUpcomingAsync(Guid currentUserId)
    {
        var now = DateTime.UtcNow;
        var myGroupIds = await _access.GetMyGroupIdsAsync(currentUserId);

        // Group events show up only for that group's members.
        var upcoming = await _events.GetAll(
                filter: e => e.StartsAt >= now && (e.GroupId == null || myGroupIds.Contains(e.GroupId.Value)),
                orderBy: e => e.StartsAt,
                asNoTracking: true,
                includes: AttendeeIncludes)
            .ToListAsync();

        return await ToDtosAsync(upcoming, currentUserId);
    }

    public async Task<List<GetEventDto>> GetGroupEventsAsync(Guid currentUserId, Guid groupId)
    {
        if (!await _access.IsGroupMemberAsync(groupId, currentUserId))
            throw new ForbiddenException("Grubun etkinliklerini görmek için üye olmalısın.");

        var now = DateTime.UtcNow;

        var upcoming = await _events.GetAll(
                filter: e => e.GroupId == groupId && e.StartsAt >= now,
                orderBy: e => e.StartsAt,
                asNoTracking: true,
                includes: AttendeeIncludes)
            .ToListAsync();

        return await ToDtosAsync(upcoming, currentUserId);
    }

    public async Task<List<GetEventDto>> GetMineAsync(Guid currentUserId, string scope)
    {
        var now = DateTime.UtcNow;

        // "created": everything I organized, upcoming or finished. "past": finished events I was going to / interested in.
        Expression<Func<Event, bool>> filter = scope switch
        {
            "created" => e => e.CreatedByUserId == currentUserId,
            "past" => e => e.StartsAt < now && e.Attendees.Any(a => a.UserId == currentUserId),
            _ => throw new BadRequestException("Scope must be either 'created' or 'past'.")
        };

        var myGroupIds = await _access.GetMyGroupIdsAsync(currentUserId);

        var mine = await _events.GetAll(filter: filter, asNoTracking: true, includes: AttendeeIncludes)
            .Where(e => e.GroupId == null || myGroupIds.Contains(e.GroupId.Value))
            .OrderByDescending(e => e.StartsAt)
            .Take(MaxMineEvents)
            .ToListAsync();

        return await ToDtosAsync(mine, currentUserId);
    }

    public async Task<GetEventDto> UpdateAsync(Guid currentUserId, Guid eventId, PutEventDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (found.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Only the creator can edit this event.");

        if (found.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("This event has already started and can no longer be edited.");

        // The limit can't drop below the people who already said they're coming.
        var goingNow = found.Attendees.Count(a => a.Status == EventAttendeeStatus.Going);
        if (dto.Capacity.HasValue && dto.Capacity.Value < goingNow)
            throw new BadRequestException($"Kontenjan, katılacağını söyleyen {goingNow} kişiden az olamaz.");

        var title = dto.Title.Trim();
        var location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        var onlineLink = CleanOnlineLink(dto);

        // Only changes that affect whether people can still make it are worth a notification.
        var importantChange = found.Title != title || found.StartsAt != dto.StartsAt || found.Location != location
            || found.IsOnline != dto.IsOnline || found.OnlineLink != onlineLink;

        var oldCoverUrl = found.CoverImageUrl;
        if (dto.CoverImage != null)
            found.CoverImageUrl = await _files.SaveImageAsync(dto.CoverImage, "events");
        else if (dto.RemoveCoverImage)
            found.CoverImageUrl = null;

        found.Title = title;
        found.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        found.Location = location;
        found.StartsAt = dto.StartsAt;
        found.Capacity = dto.Capacity;
        found.IsOnline = dto.IsOnline;
        found.OnlineLink = onlineLink;
        await _events.SaveChangesAsync();

        if (found.CoverImageUrl != oldCoverUrl)
            _files.Delete(oldCoverUrl);

        if (importantChange)
        {
            var audience = await _access.FilterViewersAsync(found, found.Attendees.Select(a => a.UserId).Where(id => id != currentUserId));
            foreach (var userId in audience)
                await _notifications.CreateAsync(userId, currentUserId, NotificationType.EventUpdated, eventId: eventId);
        }

        return await ToDetailDtoAsync(found, currentUserId);
    }

    public async Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        var existing = found.Attendees.FirstOrDefault(a => a.UserId == currentUserId);

        // Leaving ("None") is always fine; joining an event that has already started is not.
        if (dto.Status != "None" && found.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("This event has already started.");

        if (dto.Status == "None")
        {
            if (existing != null)
            {
                _attendees.Delete(existing);
                found.Attendees.Remove(existing);
            }
        }
        else
        {
            var status = Enum.Parse<EventAttendeeStatus>(dto.Status);

            // Taking a spot on a capped event: refuse when full, and bump AttendanceVersion so that two people
            // grabbing the last spot at the same moment can't both succeed (the second save hits a 409).
            if (status == EventAttendeeStatus.Going && existing?.Status != EventAttendeeStatus.Going && found.Capacity.HasValue)
            {
                var goingNow = found.Attendees.Count(a => a.Status == EventAttendeeStatus.Going);
                if (goingNow >= found.Capacity.Value)
                    throw new BadRequestException("Etkinlik dolu. Yine de 'İlgileniyorum' diyebilirsin.");

                found.AttendanceVersion++;
            }

            if (existing != null)
            {
                existing.Status = status;
            }
            else
            {
                var attendee = new EventAttendee { EventId = eventId, UserId = currentUserId, Status = status };
                await _attendees.AddAsync(attendee);
            }

            // Answering in any way (going or interested) uses up a pending invite.
            var invite = await _invites.GetAll(i => i.EventId == eventId && i.InvitedUserId == currentUserId).FirstOrDefaultAsync();
            if (invite != null)
                _invites.Delete(invite);
        }

        await _attendees.SaveChangesAsync();

        return await ToDetailDtoAsync(found, currentUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (found.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Only the creator can delete this event.");

        // Only people who still had something to attend need to hear about a cancellation.
        var audience = found.StartsAt > DateTime.UtcNow
            ? await _access.FilterViewersAsync(found, found.Attendees.Select(a => a.UserId).Where(id => id != currentUserId))
            : new List<Guid>();

        _events.Delete(found);
        await _events.SaveChangesAsync();

        _files.Delete(found.CoverImageUrl);
        await _notifications.DeleteByEventAsync(eventId);

        // The event row is gone, so the notification carries a snapshot of its title instead of an EventId.
        foreach (var userId in audience)
            await _notifications.CreateAsync(userId, currentUserId, NotificationType.EventCancelled, subject: found.Title);
    }

    // Same as ToDto plus the organizer's name/avatar — one extra lookup, so only for single-event responses.
    private async Task<GetEventDto> ToDetailDtoAsync(Event source, Guid currentUserId)
    {
        var dto = ToDto(source, currentUserId);
        var organizer = await _users.GetSummaryAsync(source.CreatedByUserId);
        dto.CreatedByName = organizer?.FullName;
        dto.CreatedByAvatarUrl = organizer?.AvatarUrl;

        if (source.GroupId.HasValue)
            dto.GroupName = (await _groups.GetByIdAsync(source.GroupId.Value))?.Name;

        // A pending invite to this event: lets the page show "X seni davet etti" with accept / decline.
        if (!dto.IsOwner && dto.CurrentUserStatus == "None")
        {
            var inviterId = await _invites
                .GetAll(i => i.EventId == source.Id && i.InvitedUserId == currentUserId, asNoTracking: true)
                .Select(i => (Guid?)i.InvitedByUserId)
                .FirstOrDefaultAsync();
            if (inviterId.HasValue)
                dto.InvitedByName = (await _users.GetSummaryAsync(inviterId.Value))?.FullName;
        }

        return dto;
    }

    // List version of ToDto: also fills the group names with one batched lookup instead of one per event.
    private async Task<List<GetEventDto>> ToDtosAsync(IEnumerable<Event> events, Guid currentUserId)
    {
        var dtos = events.Select(e => ToDto(e, currentUserId)).ToList();

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

    private GetEventDto ToDto(Event source, Guid currentUserId)
    {
        var dto = _mapper.Map<GetEventDto>(source);
        var mine = source.Attendees.FirstOrDefault(a => a.UserId == currentUserId);
        dto.CurrentUserStatus = mine?.Status.ToString() ?? "None";
        dto.IsOwner = source.CreatedByUserId == currentUserId;

        var going = source.Attendees.Count(a => a.Status == EventAttendeeStatus.Going);
        dto.IsFull = source.Capacity.HasValue && going >= source.Capacity.Value;
        dto.SpotsLeft = source.Capacity.HasValue ? Math.Max(source.Capacity.Value - going, 0) : null;

        // The meeting link is for the people who are actually attending, not for everyone browsing events.
        if (source.IsOnline && (dto.IsOwner || mine?.Status == EventAttendeeStatus.Going))
            dto.OnlineLink = source.OnlineLink;

        return dto;
    }

    // Offline events never keep a link, even if the client sent one. The stored form is Uri.AbsoluteUri, which
    // percent-encodes characters like quotes and spaces, so it is safe to put in an href.
    private static string? CleanOnlineLink(EventInputDto dto)
    {
        if (!dto.IsOnline || string.IsNullOrWhiteSpace(dto.OnlineLink))
            return null;

        if (!Uri.TryCreate(dto.OnlineLink.Trim(), UriKind.Absolute, out var uri))
            throw new BadRequestException("Bağlantı geçerli bir adres olmalı.");

        if (uri.AbsoluteUri.Length > MaxOnlineLinkLength)
            throw new BadRequestException("Bağlantı çok uzun.");

        return uri.AbsoluteUri;
    }
}
