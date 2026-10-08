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
    // Types where every occurrence is genuinely new (each counter-offer, each edit, each invitation...), so two
    // look-alike notifications are never collapsed into one. Every other type is de-duplicated per actor and target:
    // like -> unlike -> like again, or join -> leave -> join again, shouldn't spam the recipient.
    private static readonly HashSet<NotificationType> RepeatableTypes = new()
    {
        NotificationType.MarketplaceOfferReceived,
        NotificationType.MarketplaceOfferCountered,
        NotificationType.MarketplaceOfferAccepted,
        NotificationType.MarketplaceOfferRejected,
        NotificationType.MarketplaceOfferWithdrawn,
        NotificationType.MarketplaceOfferClosed,
        NotificationType.MarketplaceRatingReceived,
        NotificationType.EventUpdated,
        NotificationType.EventCancelled,
        NotificationType.EventInviteReceived,
        NotificationType.EventAnnouncement,
        NotificationType.EventWaitlistPromoted,
        NotificationType.EventReminder
    };

    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IGroupRepository _groups;
    private readonly IListingOfferRepository _offers;
    private readonly IEventRepository _events;
    private readonly IRealTimeNotifier _notifier;
    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IGroupRepository groups,
        IListingOfferRepository offers,
        IEventRepository events,
        IRealTimeNotifier notifier,
        IMapper mapper)
    {
        _notifications = notifications;
        _users = users;
        _groups = groups;
        _offers = offers;
        _events = events;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null, Guid? groupId = null,
        Guid? offerId = null, decimal? amount = null, Guid? eventId = null, string? subject = null)
    {
        // Nobody is notified about their own action, except reminders, which have no real actor (the organizer is
        // named as one so the notification has an origin, and the organizer gets reminded of their own event too).
        if (recipientId == actorId && type != NotificationType.EventReminder)
            return;

        // The recipient may have switched this kind off in their settings: then there is nothing to create or push.
        var preferences = await _users.GetNotificationPreferencesAsync(recipientId);
        if (preferences != null && !preferences.Allows(type))
            return;

        if (!RepeatableTypes.Contains(type))
        {
            var alreadyExists = await _notifications.AnyAsync(n =>
                n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type &&
                n.PostId == postId && n.CommentId == commentId && n.FriendRequestId == friendRequestId && n.GroupId == groupId &&
                n.EventId == eventId);
            if (alreadyExists)
                return;
        }

        var notification = new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId,
            CommentId = commentId,
            FriendRequestId = friendRequestId,
            GroupId = groupId,
            OfferId = offerId,
            EventId = eventId,
            Subject = subject,
            Amount = amount
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        var actor = await _users.GetSummaryAsync(actorId);
        var dto = _mapper.Map<GetNotificationDto>(notification);
        dto.ActorName = actor?.FullName ?? string.Empty;
        dto.ActorAvatarUrl = actor?.AvatarUrl;

        if (groupId.HasValue)
        {
            var group = await _groups.GetByIdAsync(groupId.Value);
            dto.GroupName = group?.Name;
        }

        if (offerId.HasValue)
        {
            var offer = await _offers.GetAll(o => o.Id == offerId.Value, asNoTracking: true, includes: "Listing").FirstOrDefaultAsync();
            dto.ListingId = offer?.ListingId;
            dto.ListingTitle = offer?.Listing?.Title;
        }

        if (subject != null)
            dto.EventTitle = subject;

        if (eventId.HasValue)
        {
            var linkedEvent = await _events.GetByIdAsync(eventId.Value);
            dto.EventTitle = linkedEvent?.Title;
            dto.EventStartsAt = linkedEvent?.StartsAt;
        }

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

        var groupIds = items.Where(n => n.GroupId.HasValue).Select(n => n.GroupId!.Value).Distinct().ToList();
        var groupNames = groupIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _groups.GetAll(g => groupIds.Contains(g.Id), asNoTracking: true).ToDictionaryAsync(g => g.Id, g => g.Name);

        // Marketplace notifications show the listing's title: one batched lookup for the whole page.
        var offerIds = items.Where(n => n.OfferId.HasValue).Select(n => n.OfferId!.Value).Distinct().ToList();
        var offerListings = offerIds.Count == 0
            ? new Dictionary<Guid, (Guid ListingId, string Title)>()
            : (await _offers.GetAll(o => offerIds.Contains(o.Id), asNoTracking: true, includes: "Listing").ToListAsync())
                .Where(o => o.Listing != null)
                .ToDictionary(o => o.Id, o => (o.ListingId, o.Listing!.Title));

        var eventIds = items.Where(n => n.EventId.HasValue).Select(n => n.EventId!.Value).Distinct().ToList();
        var eventInfo = eventIds.Count == 0
            ? new Dictionary<Guid, (string Title, DateTime StartsAt)>()
            : await _events.GetAll(e => eventIds.Contains(e.Id), asNoTracking: true).ToDictionaryAsync(e => e.Id, e => (e.Title, e.StartsAt));

        var dtos = _mapper.Map<List<GetNotificationDto>>(items);

        for (var i = 0; i < dtos.Count; i++)
        {
            if (actors.TryGetValue(items[i].ActorId, out var actor))
            {
                dtos[i].ActorName = actor.FullName;
                dtos[i].ActorAvatarUrl = actor.AvatarUrl;
            }

            if (items[i].GroupId.HasValue && groupNames.TryGetValue(items[i].GroupId!.Value, out var groupName))
                dtos[i].GroupName = groupName;

            if (items[i].Subject != null)
                dtos[i].EventTitle = items[i].Subject;

            if (items[i].EventId.HasValue && eventInfo.TryGetValue(items[i].EventId!.Value, out var info))
            {
                dtos[i].EventTitle = info.Title;
                dtos[i].EventStartsAt = info.StartsAt;
            }

            if (items[i].OfferId.HasValue && offerListings.TryGetValue(items[i].OfferId!.Value, out var offerListing))
            {
                dtos[i].ListingId = offerListing.ListingId;
                dtos[i].ListingTitle = offerListing.Title;
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

    public async Task DeleteByEventAsync(Guid eventId)
    {
        var related = await _notifications.GetAll(n => n.EventId == eventId).ToListAsync();
        if (related.Count == 0)
            return;

        foreach (var notification in related)
            _notifications.Delete(notification);

        await _notifications.SaveChangesAsync();
    }
}
