using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface INotificationService
{
    // Called by other services when a like/comment/friend event happens; saves, then pushes over SignalR.
    // Does nothing when actor and recipient are the same person.
    Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null, Guid? groupId = null,
        Guid? offerId = null, decimal? amount = null, Guid? eventId = null, string? subject = null, Guid? storyId = null,
        Guid? gameId = null);

    Task<PagedResult<GetNotificationDto>> GetMyNotificationsAsync(Guid currentUserId, int page, int pageSize);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    Task MarkAsReadAsync(Guid currentUserId, Guid notificationId);

    Task MarkAllAsReadAsync(Guid currentUserId);

    // Cleans up notifications that point at a post that no longer exists.
    Task DeleteByPostAsync(Guid postId);

    // The same for many posts at once (a group's whole wall, or everything a deleted account wrote).
    Task DeleteByPostsAsync(IReadOnlyCollection<Guid> postIds);

    // Same for notifications pointing at an event that was deleted.
    Task DeleteByEventAsync(Guid eventId);

    // Same for notifications about a group that was deleted (invites, join requests, role changes...).
    Task DeleteByGroupAsync(Guid groupId);
}
