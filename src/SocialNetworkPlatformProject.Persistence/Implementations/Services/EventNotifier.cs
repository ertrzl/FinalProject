using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventNotifier : IEventNotifier
{
    private readonly INotificationService _notifications;
    private readonly IEventAccessService _access;

    public EventNotifier(INotificationService notifications, IEventAccessService access)
    {
        _notifications = notifications;
        _access = access;
    }

    public Task JoinedAsync(Event ev, Guid userId)
    {
        return _notifications.CreateAsync(ev.CreatedByUserId, userId, NotificationType.EventJoined, eventId: ev.Id);
    }

    public async Task PromotedAsync(Event ev, IEnumerable<EventAttendee> promoted, Guid actingUserId)
    {
        foreach (var attendee in promoted)
        {
            if (attendee.UserId != actingUserId)
                await _notifications.CreateAsync(attendee.UserId, ev.CreatedByUserId, NotificationType.EventWaitlistPromoted, eventId: ev.Id);

            await JoinedAsync(ev, attendee.UserId);
        }
    }

    public async Task UpdatedAsync(Event ev, Guid organizerId)
    {
        foreach (var userId in await AttendeesAsync(ev, organizerId))
            await _notifications.CreateAsync(userId, organizerId, NotificationType.EventUpdated, eventId: ev.Id);
    }

    public async Task CancelledAsync(Event ev, IReadOnlyCollection<Guid> attendeeIds, Guid organizerId)
    {
        // Nobody needs to hear that something that is already over was removed.
        if (ev.EndsAt <= DateTime.UtcNow)
            return;

        // The event row is gone, so the notification carries a snapshot of its title instead of an EventId.
        foreach (var userId in await _access.FilterViewersAsync(ev, attendeeIds.Where(id => id != organizerId)))
            await _notifications.CreateAsync(userId, organizerId, NotificationType.EventCancelled, subject: ev.Title);
    }

    public async Task AnnouncementAsync(Event ev, Guid organizerId)
    {
        foreach (var userId in await AttendeesAsync(ev, organizerId))
            await _notifications.CreateAsync(userId, organizerId, NotificationType.EventAnnouncement, eventId: ev.Id);
    }

    public Task CommentAddedAsync(Event ev, Guid commentId, Guid authorId)
    {
        return _notifications.CreateAsync(ev.CreatedByUserId, authorId, NotificationType.EventCommentAdded, commentId: commentId, eventId: ev.Id);
    }

    public Task InvitedAsync(Event ev, Guid invitedUserId, Guid invitedByUserId)
    {
        return _notifications.CreateAsync(invitedUserId, invitedByUserId, NotificationType.EventInviteReceived, eventId: ev.Id);
    }

    public async Task ReminderAsync(Event ev)
    {
        var going = ev.Attendees.Where(a => a.Status == EventAttendeeStatus.Going).Select(a => a.UserId);
        var recipients = await _access.FilterViewersAsync(ev, going.Append(ev.CreatedByUserId));

        foreach (var userId in recipients)
            await _notifications.CreateAsync(userId, ev.CreatedByUserId, NotificationType.EventReminder, eventId: ev.Id);
    }

    // Everyone with a response on the event (going, interested or waiting) who can still see it, minus the person acting.
    private Task<List<Guid>> AttendeesAsync(Event ev, Guid exceptUserId)
    {
        return _access.FilterViewersAsync(ev, ev.Attendees.Select(a => a.UserId).Where(id => id != exceptUserId));
    }
}
