using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Every notification an event sends, in one place: who is told, about what, and who never hears about their own
// actions. The event services call these instead of building notifications themselves. Methods that notify "the
// attendees" expect ev.Attendees to be loaded.
public interface IEventNotifier
{
    // To the organizer: someone is going.
    Task JoinedAsync(Event ev, Guid userId);

    // To everyone who moved up from the waiting list (except the person whose own action it was), and to the
    // organizer about each of them being in now.
    Task PromotedAsync(Event ev, IEnumerable<EventAttendee> promoted, Guid actingUserId);

    // To every attendee: the organizer changed the title, time or place.
    Task UpdatedAsync(Event ev, Guid organizerId);

    // To every attendee, when the event was deleted before it finished. The event row is already gone by then, so
    // the caller passes who was attending (read before deleting).
    Task CancelledAsync(Event ev, IReadOnlyCollection<Guid> attendeeIds, Guid organizerId);

    // To every attendee: the organizer posted an announcement.
    Task AnnouncementAsync(Event ev, Guid organizerId);

    // To the organizer: someone commented.
    Task CommentAddedAsync(Event ev, Guid commentId, Guid authorId);

    // To the invited person.
    Task InvitedAsync(Event ev, Guid invitedUserId, Guid invitedByUserId);

    // To the people going, and the organizer: it is about to start.
    Task ReminderAsync(Event ev);
}
