using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventNotifier
{
    Task JoinedAsync(Event ev, Guid userId);

    Task PromotedAsync(Event ev, IEnumerable<EventAttendee> promoted, Guid actingUserId);

    Task UpdatedAsync(Event ev, Guid organizerId);

    Task CancelledAsync(Event ev, IReadOnlyCollection<Guid> attendeeIds, Guid organizerId);

    Task AnnouncementAsync(Event ev, Guid organizerId);

    Task CommentAddedAsync(Event ev, Guid commentId, Guid authorId);

    Task InvitedAsync(Event ev, Guid invitedUserId, Guid invitedByUserId);

    Task ReminderAsync(Event ev);
}
