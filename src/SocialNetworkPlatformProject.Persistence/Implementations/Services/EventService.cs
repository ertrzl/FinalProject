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
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private const int MaxOnlineLinkLength = 500;

    private readonly IEventRepository _events;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventInviteRepository _invites;
    private readonly IEventPhotoRepository _photos;
    private readonly IEventAccessService _access;
    private readonly IEventDtoBuilder _builder;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IEventNotifier _notifier;

    public EventService(
        IEventRepository events,
        IEventAttendeeRepository attendees,
        IEventInviteRepository invites,
        IEventPhotoRepository photos,
        IEventAccessService access,
        IEventDtoBuilder builder,
        IFileStorageService files,
        INotificationService notifications,
        IEventNotifier notifier)
    {
        _events = events;
        _attendees = attendees;
        _invites = invites;
        _photos = photos;
        _access = access;
        _builder = builder;
        _files = files;
        _notifications = notifications;
        _notifier = notifier;
    }

    public async Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto)
    {
        if (dto.GroupId.HasValue)
            await _access.EnsureCanCreateGroupEventAsync(currentUserId, dto.GroupId.Value);

        var (latitude, longitude) = ResolveCoordinates(dto);

        string? coverUrl = null;
        if (dto.CoverImage != null)
            coverUrl = await _files.SaveImageAsync(dto.CoverImage, "events");

        var (startsAt, endsAt) = ResolveTimes(dto);

        var newEvent = new Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location?.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            CoverImageUrl = coverUrl,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Capacity = dto.Capacity,
            IsOnline = dto.IsOnline,
            OnlineLink = CleanOnlineLink(dto),
            IsPrivate = dto.IsPrivate,
            GroupId = dto.GroupId,
            CreatedByUserId = currentUserId
        };
        newEvent.ResetReminders(DateTime.UtcNow);

        await _events.AddAsync(newEvent);
        await _events.SaveChangesAsync();

        return await _builder.BuildDetailAsync(newEvent, currentUserId);
    }

    public async Task<GetEventDto> UpdateAsync(Guid currentUserId, Guid eventId, PutEventDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (found.CreatedByUserId != currentUserId)
            throw new ForbiddenException(Loc.T("event.editCreatorOnly"));

        var now = DateTime.UtcNow;
        if (found.EndsAt <= now)
            throw new BadRequestException(Loc.T("event.editEnded"));

        var (startsAt, endsAt) = ResolveTimes(dto);
        var (latitude, longitude) = ResolveCoordinates(dto);
        var hasStarted = found.StartsAt <= now;
        ValidateReschedule(found, startsAt, endsAt, hasStarted, now);
        if (hasStarted)
            startsAt = found.StartsAt;

        if (dto.IsPrivate && found.GroupId.HasValue)
            throw new BadRequestException(Loc.T("event.groupNotPrivate"));

        var goingNow = found.GoingCount;
        if (dto.Capacity.HasValue && dto.Capacity.Value < goingNow)
            throw new BadRequestException(Loc.T("event.capacityBelowGoing", goingNow));

        var title = dto.Title.Trim();
        var location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        var onlineLink = CleanOnlineLink(dto);

        var importantChange = found.Title != title || found.StartsAt != startsAt || found.EndsAt != endsAt
            || found.Location != location || found.IsOnline != dto.IsOnline || found.OnlineLink != onlineLink
            || found.Latitude != latitude || found.Longitude != longitude;

        var oldCoverUrl = found.CoverImageUrl;
        string? newCoverUrl = null;
        if (dto.CoverImage != null)
        {
            newCoverUrl = await _files.SaveImageAsync(dto.CoverImage, "events");
            found.CoverImageUrl = newCoverUrl;
        }
        else if (dto.RemoveCoverImage)
        {
            found.CoverImageUrl = null;
        }

        var startMoved = found.StartsAt != startsAt;

        found.Title = title;
        found.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        found.Location = location;
        found.Latitude = latitude;
        found.Longitude = longitude;
        found.StartsAt = startsAt;
        found.EndsAt = endsAt;
        if (startMoved)
            found.ResetReminders(now);
        found.Capacity = dto.Capacity;
        found.IsOnline = dto.IsOnline;
        found.OnlineLink = onlineLink;
        found.IsPrivate = dto.IsPrivate;

        var promoted = found.PromoteWaitlist();

        try
        {
            await _events.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(newCoverUrl);
            throw;
        }

        if (found.CoverImageUrl != oldCoverUrl)
            _files.Delete(oldCoverUrl);

        if (importantChange)
            await _notifier.UpdatedAsync(found, currentUserId);

        await _notifier.PromotedAsync(found, promoted, currentUserId);

        return await _builder.BuildDetailAsync(found, currentUserId);
    }

    public async Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        var existing = found.Attendees.FirstOrDefault(a => a.UserId == currentUserId);
        var wasGoing = existing?.Status == EventAttendeeStatus.Going;

        if (dto.Status != "None" && found.EndsAt <= DateTime.UtcNow)
            throw new BadRequestException(Loc.T("event.ended"));

        var newStatus = dto.Status == "None" ? (EventAttendeeStatus?)null : Enum.Parse<EventAttendeeStatus>(dto.Status);

        if (newStatus == null)
        {
            if (existing != null)
            {
                _attendees.Delete(existing);
                found.Attendees.Remove(existing);
            }
        }
        else
        {
            EnsureCanTakeStatus(found, existing, newStatus.Value);

            if (existing != null)
            {
                existing.Status = newStatus.Value;
                existing.WaitlistedAt = newStatus == EventAttendeeStatus.Waitlisted ? existing.WaitlistedAt ?? DateTime.UtcNow : null;

                if (newStatus != EventAttendeeStatus.Going)
                {
                    existing.CheckedInAt = null;
                    existing.CheckedInByUserId = null;
                }
            }
            else
            {
                await _attendees.AddAsync(new EventAttendee
                {
                    EventId = eventId,
                    UserId = currentUserId,
                    Status = newStatus.Value,
                    WaitlistedAt = newStatus == EventAttendeeStatus.Waitlisted ? DateTime.UtcNow : null
                });
            }

            var invite = await _invites.GetAll(i => i.EventId == eventId && i.InvitedUserId == currentUserId).FirstOrDefaultAsync();
            if (invite != null)
                _invites.Delete(invite);
        }

        if (wasGoing && newStatus != EventAttendeeStatus.Going && found.Capacity.HasValue)
            found.AttendanceVersion++;
        var promoted = found.PromoteWaitlist();

        await _attendees.SaveChangesAsync();

        if (newStatus == EventAttendeeStatus.Going && !wasGoing)
            await _notifier.JoinedAsync(found, currentUserId);

        await _notifier.PromotedAsync(found, promoted, currentUserId);

        return await _builder.BuildDetailAsync(found, currentUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId, bool asAdmin = false)
    {
        var found = asAdmin
            ? await _events.GetByIdAsync(eventId, AttendeeIncludes) ?? throw new NotFoundException(Loc.T("event.notFound"))
            : await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (!asAdmin && found.CreatedByUserId != currentUserId)
            throw new ForbiddenException(Loc.T("event.deleteCreatorOnly"));

        var attendeeIds = found.Attendees.Select(a => a.UserId).ToList();
        var photoUrls = await _photos.GetUrlsByEventAsync(eventId);

        _events.Delete(found);
        await _events.SaveChangesAsync();

        _files.Delete(found.CoverImageUrl);
        foreach (var url in photoUrls)
            _files.Delete(url);
        await _notifications.DeleteByEventAsync(eventId);
        await _notifier.CancelledAsync(found, attendeeIds, currentUserId);
    }

    public async Task RemoveUserFromGroupEventsAsync(Guid userId, Guid groupId, Guid groupOwnerId)
    {
        var now = DateTime.UtcNow;

        var affected = await _events.GetAll(
                filter: e => e.GroupId == groupId && e.EndsAt > now
                    && (e.CreatedByUserId == userId || e.Attendees.Any(a => a.UserId == userId)),
                includes: AttendeeIncludes)
            .ToListAsync();

        var promotions = new List<(Event Event, IReadOnlyList<EventAttendee> Promoted)>();
        foreach (var ev in affected)
        {
            if (ev.CreatedByUserId == userId)
                ev.CreatedByUserId = groupOwnerId;

            var attendee = ev.Attendees.FirstOrDefault(a => a.UserId == userId);
            if (attendee == null)
                continue;

            var wasGoing = attendee.Status == EventAttendeeStatus.Going;
            _attendees.Delete(attendee);
            ev.Attendees.Remove(attendee);

            if (wasGoing && ev.Capacity.HasValue)
                ev.AttendanceVersion++;

            var promoted = ev.PromoteWaitlist();
            if (promoted.Count > 0)
                promotions.Add((ev, promoted));
        }

        var invites = await _invites.GetAll(i => i.InvitedUserId == userId && i.Event!.GroupId == groupId).ToListAsync();
        foreach (var invite in invites)
            _invites.Delete(invite);

        await _events.SaveChangesAsync();

        foreach (var (ev, promoted) in promotions)
            await _notifier.PromotedAsync(ev, promoted, actingUserId: Guid.Empty);
    }

    private static void EnsureCanTakeStatus(Event ev, EventAttendee? existing, EventAttendeeStatus status)
    {
        if (status == EventAttendeeStatus.Going && existing?.Status != EventAttendeeStatus.Going && ev.Capacity.HasValue)
        {
            if (ev.IsFull)
                throw new BadRequestException(Loc.T("event.full"));

            ev.AttendanceVersion++;
        }

        if (status == EventAttendeeStatus.Waitlisted)
        {
            if (existing?.Status == EventAttendeeStatus.Going)
                throw new BadRequestException(Loc.T("event.alreadyGoing"));

            if (!ev.Capacity.HasValue)
                throw new BadRequestException(Loc.T("event.noCapacity"));

            if (existing?.Status != EventAttendeeStatus.Waitlisted)
                ev.AttendanceVersion++;
        }
    }

    private static (DateTime StartsAt, DateTime EndsAt) ResolveTimes(EventInputDto dto)
    {
        var startsAt = dto.StartsAt.ToUtc();
        var endsAt = dto.EndsAt?.ToUtc() ?? startsAt.Add(EventLimits.DefaultDuration);
        return (startsAt, endsAt);
    }

    private static (double? Latitude, double? Longitude) ResolveCoordinates(EventInputDto dto)
    {
        if (dto.IsOnline)
            return (null, null);

        if (!EventCoordinates.TryParse(dto.Latitude, dto.Longitude, out var latitude, out var longitude))
            throw new BadRequestException(Loc.T("val.event.coordinates"));

        return (latitude, longitude);
    }

    private static void ValidateReschedule(Event ev, DateTime startsAt, DateTime endsAt, bool hasStarted, DateTime now)
    {
        if (hasStarted)
        {
            if (Math.Abs((startsAt - ev.StartsAt).TotalMinutes) >= 1)
                throw new BadRequestException(Loc.T("event.startLocked"));
        }
        else if (startsAt <= now)
        {
            throw new BadRequestException(Loc.T("event.startFuture"));
        }

        if (endsAt <= now)
            throw new BadRequestException(Loc.T("event.endPast"));
    }

    private static string? CleanOnlineLink(EventInputDto dto)
    {
        if (!dto.IsOnline || string.IsNullOrWhiteSpace(dto.OnlineLink))
            return null;

        if (!Uri.TryCreate(dto.OnlineLink.Trim(), UriKind.Absolute, out var uri))
            throw new BadRequestException(Loc.T("event.linkInvalid"));

        if (uri.AbsoluteUri.Length > MaxOnlineLinkLength)
            throw new BadRequestException(Loc.T("event.linkTooLong"));

        return uri.AbsoluteUri;
    }
}
