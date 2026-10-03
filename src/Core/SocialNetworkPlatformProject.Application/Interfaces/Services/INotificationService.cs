using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface INotificationService
{
    // Called by other services when a like/comment/friend event happens; saves, then pushes over SignalR.
    // Does nothing when actor and recipient are the same person.
    Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null, Guid? groupId = null,
        Guid? offerId = null, decimal? amount = null, Guid? eventId = null, string? subject = null);

    Task<PagedResult<GetNotificationDto>> GetMyNotificationsAsync(Guid currentUserId, int page, int pageSize);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    Task MarkAsReadAsync(Guid currentUserId, Guid notificationId);

    Task MarkAllAsReadAsync(Guid currentUserId);

    // Cleans up notifications that point at a post that no longer exists.
    Task DeleteByPostAsync(Guid postId);

    // Same for notifications pointing at an event that was deleted.
    Task DeleteByEventAsync(Guid eventId);
}
