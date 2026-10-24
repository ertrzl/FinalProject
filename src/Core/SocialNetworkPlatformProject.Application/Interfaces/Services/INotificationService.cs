using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface INotificationService
{
    Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null, Guid? groupId = null,
        Guid? offerId = null, decimal? amount = null, Guid? eventId = null, string? subject = null, Guid? storyId = null,
        Guid? gameId = null, Guid? listingId = null, decimal? previousAmount = null, Guid? conversationId = null);

    Task<PagedResult<GetNotificationDto>> GetMyNotificationsAsync(Guid currentUserId, int page, int pageSize);

    Task<int> GetUnreadCountAsync(Guid currentUserId);

    Task MarkAsReadAsync(Guid currentUserId, Guid notificationId);

    Task MarkAllAsReadAsync(Guid currentUserId);

    Task DeleteByPostAsync(Guid postId);

    Task DeleteByPostsAsync(IReadOnlyCollection<Guid> postIds);

    Task DeleteByEventAsync(Guid eventId);

    Task DeleteByGroupAsync(Guid groupId);

    Task DeleteByListingAsync(Guid listingId);
}
