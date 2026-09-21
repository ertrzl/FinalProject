using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IRealTimeNotifier _notifier;
    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IRealTimeNotifier notifier,
        IMapper mapper)
    {
        _notifications = notifications;
        _users = users;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null)
    {
        if (recipientId == actorId)
            return;

        // Like -> unlike -> like again shouldn't spam the recipient with identical entries.
        var alreadyExists = await _notifications.AnyAsync(n =>
            n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type &&
            n.PostId == postId && n.CommentId == commentId && n.FriendRequestId == friendRequestId);
        if (alreadyExists)
            return;

        var notification = new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId,
            CommentId = commentId,
            FriendRequestId = friendRequestId
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        var actor = await _users.GetSummaryAsync(actorId);
        var dto = _mapper.Map<GetNotificationDto>(notification);
        dto.ActorName = actor?.FullName ?? string.Empty;
        dto.ActorAvatarUrl = actor?.AvatarUrl;

        await _notifier.SendNotificationAsync(recipientId, dto);
    }

    public async Task<PagedResult<GetNotificationDto>> GetMyNotificationsAsync(Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _notifications.GetAll(n => n.RecipientId == currentUserId).CountAsync();

        var items = await _notifications.GetAll(
                filter: n => n.RecipientId == currentUserId,
                orderBy: n => n.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                page: page,
                take: pageSize)
            .ToListAsync();

        var actors = await _users.GetSummariesAsync(items.Select(n => n.ActorId));
        var dtos = _mapper.Map<List<GetNotificationDto>>(items);

        for (var i = 0; i < dtos.Count; i++)
        {
            if (actors.TryGetValue(items[i].ActorId, out var actor))
            {
                dtos[i].ActorName = actor.FullName;
                dtos[i].ActorAvatarUrl = actor.AvatarUrl;
            }
        }

        return new PagedResult<GetNotificationDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId)
    {
        return await _notifications.GetAll(n => n.RecipientId == currentUserId && !n.IsRead).CountAsync();
    }

    public async Task MarkAsReadAsync(Guid currentUserId, Guid notificationId)
    {
        var notification = await _notifications.GetByIdAsync(notificationId)
            ?? throw new NotFoundException("Notification not found.");

        if (notification.RecipientId != currentUserId)
            throw new ForbiddenException("This notification doesn't belong to you.");

        notification.IsRead = true;
        await _notifications.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid currentUserId)
    {
        var unread = await _notifications.GetAll(n => n.RecipientId == currentUserId && !n.IsRead).ToListAsync();
        if (unread.Count == 0)
            return;

        foreach (var notification in unread)
            notification.IsRead = true;

        await _notifications.SaveChangesAsync();
    }

    public async Task DeleteByPostAsync(Guid postId)
    {
        var related = await _notifications.GetAll(n => n.PostId == postId).ToListAsync();
        if (related.Count == 0)
            return;

        foreach (var notification in related)
            _notifications.Delete(notification);

        await _notifications.SaveChangesAsync();
    }
}
