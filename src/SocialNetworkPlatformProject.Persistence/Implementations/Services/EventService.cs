using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventService : IEventService
{
    // The attendee list drives capacity checks and who gets notified.
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private const int MaxOnlineLinkLength = 500; // matches the OnlineLink column

    private readonly IEventRepository _events;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventInviteRepository _invites;
    private readonly IEventAccessService _access;
    private readonly IEventDtoBuilder _builder;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IEventNotifier _notifier;

    public EventService(
        IEventRepository events,
        IEventAttendeeRepository attendees,
        IEventInviteRepository invites,
        IEventAccessService access,
        IEventDtoBuilder builder,
        IFileStorageService files,
        INotificationService notifications,
        IEventNotifier notifier)
    {
        _events = events;
        _attendees = attendees;
        _invites = invites;
        _access = access;
        _builder = builder;
        _files = files;
        _notifications = notifications;
        _notifier = notifier;
    }

    public async Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto)
    {
        // Checked before the cover is saved, so a refused request doesn't leave an orphan file behind.
        if (dto.GroupId.HasValue)
            await _access.EnsureCanCreateGroupEventAsync(currentUserId, dto.GroupId.Value);

        string? coverUrl = null;
        if (dto.CoverImage != null)
            coverUrl = await _files.SaveImageAsync(dto.CoverImage, "events");

        var (startsAt, endsAt) = ResolveTimes(dto);

        var newEvent = new Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location?.Trim(),
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
            throw new ForbiddenException("Only the creator can edit this event.");

        var now = DateTime.UtcNow;
        if (found.EndsAt <= now)
            throw new BadRequestException("Sona ermiş bir etkinlik düzenlenemez.");

        var (startsAt, endsAt) = ResolveTimes(dto);
        var hasStarted = found.StartsAt <= now;
        ValidateReschedule(found, startsAt, endsAt, hasStarted, now);
        if (hasStarted)
            startsAt = found.StartsAt; // the rounded value the form re-sent is not a change

        if (dto.IsPrivate && found.GroupId.HasValue)
            throw new BadRequestException("Grup etkinlikleri zaten sadece grup üyelerine açıktır, ayrıca özel yapılamaz.");

        // The limit can't drop below the people who already said they're coming.
        var goingNow = found.GoingCount;
        if (dto.Capacity.HasValue && dto.Capacity.Value < goingNow)
            throw new BadRequestException($"Kontenjan, katılacağını söyleyen {goingNow} kişiden az olamaz.");

        var title = dto.Title.Trim();
        var location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        var onlineLink = CleanOnlineLink(dto);

        // Only changes that affect whether people can still make it are worth a notification.
        var importantChange = found.Title != title || found.StartsAt != startsAt || found.EndsAt != endsAt
            || found.Location != location || found.IsOnline != dto.IsOnline || found.OnlineLink != onlineLink;

        var oldCoverUrl = found.CoverImageUrl;
        if (dto.CoverImage != null)
            found.CoverImageUrl = await _files.SaveImageAsync(dto.CoverImage, "events");
        else if (dto.RemoveCoverImage)
            found.CoverImageUrl = null;

        var startMoved = found.StartsAt != startsAt;

        found.Title = title;
        found.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        found.Location = location;
        found.StartsAt = startsAt;
        found.EndsAt = endsAt;
        if (startMoved)
            found.ResetReminders(now); // the reminders are for the new time
        found.Capacity = dto.Capacity;
        found.IsOnline = dto.IsOnline;
        found.OnlineLink = onlineLink;
        found.IsPrivate = dto.IsPrivate; // switching to private keeps the people already in; everyone else loses sight of it

        // A bigger (or removed) limit opens spots for the people waiting. Same save as the edit itself.
        var promoted = found.PromoteWaitlist();
        await _events.SaveChangesAsync();

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

        // Leaving ("None") is always fine; joining is open until the event has finished (an ongoing event can still be joined).
        if (dto.Status != "None" && found.EndsAt <= DateTime.UtcNow)
            throw new BadRequestException("Bu etkinlik sona erdi.");

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
                // Staying on the waiting list keeps your place in the queue; any other change drops it.
                existing.WaitlistedAt = newStatus == EventAttendeeStatus.Waitlisted ? existing.WaitlistedAt ?? DateTime.UtcNow : null;
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

            // Answering in any way (going, interested, waiting) uses up a pending invite.
            var invite = await _invites.GetAll(i => i.EventId == eventId && i.InvitedUserId == currentUserId).FirstOrDefaultAsync();
            if (invite != null)
                _invites.Delete(invite);
        }

        // Giving up a spot, or asking for one that has just opened, is settled in this same save: whoever has waited
        // longest moves up. Bumping the version makes a competing change lose with a 409 instead of leaving a free
        // spot next to a waiting list.
        if (wasGoing && newStatus != EventAttendeeStatus.Going && found.Capacity.HasValue)
            found.AttendanceVersion++;
        var promoted = found.PromoteWaitlist();

        await _attendees.SaveChangesAsync();

        // The organizer hears about every new "Katılıyorum" (once per person: leaving and rejoining isn't news again).
        if (newStatus == EventAttendeeStatus.Going && !wasGoing)
            await _notifier.JoinedAsync(found, currentUserId);

        await _notifier.PromotedAsync(found, promoted, currentUserId);

        return await _builder.BuildDetailAsync(found, currentUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (found.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Only the creator can delete this event.");

        var attendeeIds = found.Attendees.Select(a => a.UserId).ToList(); // read before the rows are deleted

        _events.Delete(found);
        await _events.SaveChangesAsync();

        _files.Delete(found.CoverImageUrl);
        await _notifications.DeleteByEventAsync(eventId);
        await _notifier.CancelledAsync(found, attendeeIds, currentUserId);
    }

    public async Task RemoveUserFromGroupEventsAsync(Guid userId, Guid groupId, Guid groupOwnerId)
    {
        var now = DateTime.UtcNow;

        // Finished events keep their history; only what is still ahead matters.
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

            // Same rules as leaving by hand: a freed spot is settled in this save, in queue order.
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

    // The capacity rules for taking a status. Taking a spot ("Going") or queueing for one ("Waitlisted") bumps
    // AttendanceVersion, so two people grabbing the last spot at the same moment can't both succeed (the second save hits a 409).
    private static void EnsureCanTakeStatus(Event ev, EventAttendee? existing, EventAttendeeStatus status)
    {
        if (status == EventAttendeeStatus.Going && existing?.Status != EventAttendeeStatus.Going && ev.Capacity.HasValue)
        {
            if (ev.IsFull)
                throw new BadRequestException("Etkinlik dolu. Bekleme listesine katılabilir ya da 'İlgileniyorum' diyebilirsin.");

            ev.AttendanceVersion++;
        }

        if (status == EventAttendeeStatus.Waitlisted)
        {
            if (existing?.Status == EventAttendeeStatus.Going)
                throw new BadRequestException("Zaten katılıyorsun, bekleme listesine girmene gerek yok.");

            if (!ev.Capacity.HasValue)
                throw new BadRequestException("Bu etkinliğin kontenjan sınırı yok, doğrudan katılabilirsin.");

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

    // An upcoming event may be moved anywhere in the future; an ongoing one keeps its start (it already happened)
    // and may only change when it ends. Nothing may end in the past.
    private static void ValidateReschedule(Event ev, DateTime startsAt, DateTime endsAt, bool hasStarted, DateTime now)
    {
        if (hasStarted)
        {
            // The edit form re-sends the start it displays, which is rounded to the minute.
            if (Math.Abs((startsAt - ev.StartsAt).TotalMinutes) >= 1)
                throw new BadRequestException("Başlamış bir etkinliğin başlangıç zamanı değiştirilemez.");
        }
        else if (startsAt <= now)
        {
            throw new BadRequestException("Etkinlik başlangıcı gelecekte olmalı.");
        }

        if (endsAt <= now)
            throw new BadRequestException("Bitiş zamanı geçmişte olamaz.");
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
