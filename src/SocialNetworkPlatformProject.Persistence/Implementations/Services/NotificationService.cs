using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        NotificationType.ContentRemoved,
        NotificationType.GroupChatAdded, // each time somebody is added again (after being removed or leaving) it is news
        NotificationType.MarketplaceOfferReceived,
        NotificationType.MarketplaceOfferCountered,
        NotificationType.MarketplaceOfferAccepted,
        NotificationType.MarketplaceOfferRejected,
        NotificationType.MarketplaceOfferWithdrawn,
        NotificationType.MarketplaceOfferClosed,
        NotificationType.MarketplaceRatingReceived,
        NotificationType.MarketplacePriceDropped,
        NotificationType.EventUpdated,
        NotificationType.EventCancelled,
        NotificationType.EventInviteReceived,
        NotificationType.EventAnnouncement,
        NotificationType.EventWaitlistPromoted,
        NotificationType.EventReminder
    };

    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IBlockService _blocks;
    private readonly IGroupRepository _groups;
    private readonly IListingOfferRepository _offers;
    private readonly IEventRepository _events;
    private readonly IFriendRequestRepository _friendRequests;
    private readonly IGroupJoinRequestRepository _joinRequests;
    private readonly IGroupInviteRepository _invites;
    private readonly IRealTimeNotifier _notifier;
    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IBlockService blocks,
        IGroupRepository groups,
        IListingOfferRepository offers,
        IEventRepository events,
        IFriendRequestRepository friendRequests,
        IGroupJoinRequestRepository joinRequests,
        IGroupInviteRepository invites,
        IRealTimeNotifier notifier,
        IMapper mapper)
    {
        _notifications = notifications;
        _users = users;
        _blocks = blocks;
        _groups = groups;
        _offers = offers;
        _events = events;
        _friendRequests = friendRequests;
        _joinRequests = joinRequests;
        _invites = invites;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task CreateAsync(Guid recipientId, Guid actorId, NotificationType type,
        Guid? postId = null, Guid? commentId = null, Guid? friendRequestId = null, Guid? groupId = null,
        Guid? offerId = null, decimal? amount = null, Guid? eventId = null, string? subject = null, Guid? storyId = null,
        Guid? gameId = null, Guid? listingId = null, decimal? previousAmount = null, Guid? conversationId = null)
    {
        // Nobody is notified about their own action, except reminders, which have no real actor (the organizer is
        // named as one so the notification has an origin, and the organizer gets reminded of their own event too).
        if (recipientId == actorId && type != NotificationType.EventReminder)
            return;

        // Nothing reaches somebody from a person they blocked or who blocked them. A notice from the admins is not
        // from a person: it always arrives.
        if (type != NotificationType.ContentRemoved && recipientId != actorId && await _blocks.AreBlockedAsync(recipientId, actorId))
            return;

        // The recipient may have switched this kind off in their settings: then there is nothing to create or push.
        var preferences = await _users.GetNotificationPreferencesAsync(recipientId);
        if (preferences != null && !preferences.Allows(type))
            return;

        // The recipient muted the group this comes from (a wall post and what happens under it, a group event). A notice
        // from the site's admins is not the group's activity: it always arrives.
        if (type != NotificationType.ContentRemoved && await _groups.IsActivityMutedAsync(recipientId, postId, eventId))
            return;

        if (!RepeatableTypes.Contains(type))
        {
            var alreadyExists = await _notifications.AnyAsync(n =>
                n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type &&
                n.PostId == postId && n.CommentId == commentId && n.FriendRequestId == friendRequestId && n.GroupId == groupId &&
                n.EventId == eventId && n.StoryId == storyId && n.GameId == gameId && n.ConversationId == conversationId);
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
            StoryId = storyId,
            GameId = gameId,
            ConversationId = conversationId,
            ListingId = listingId,
            Subject = subject,
            Amount = amount,
            PreviousAmount = previousAmount
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        var actor = await _users.GetSummaryAsync(actorId);
        var dto = _mapper.Map<GetNotificationDto>(notification);
        dto.ActorName = actor?.FullName ?? string.Empty;
        dto.ActorAvatarUrl = actor?.AvatarUrl;
        if (type == NotificationType.ContentRemoved)
            HideActor(dto);

        // A request is created together with its notification, so it is waiting for an answer at this very moment.
        dto.IsPending = IsRequestType(type);

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

        // The subject holds a title for event notifications and the emoji's name for a reaction to a post.
        if (subject != null && type == NotificationType.PostLiked)
            dto.Reaction = subject;
        else if (subject != null && type == NotificationType.MarketplacePriceDropped)
            dto.ListingTitle = subject;
        else if (subject != null)
            dto.EventTitle = subject;

        if (eventId.HasValue)
        {
            var linkedEvent = await _events.GetByIdAsync(eventId.Value);
            dto.EventTitle = linkedEvent?.Title;
            dto.EventStartsAt = linkedEvent?.StartsAt;
        }

        await _notifier.SendNotificationAsync(recipientId, dto);
    }

    private static bool IsRequestType(NotificationType type) =>
        type is NotificationType.FriendRequestReceived or NotificationType.GroupJoinRequestReceived or NotificationType.GroupInviteReceived;

    // Which of the request notifications on this page are still waiting for an answer: one batched lookup per kind.
    // Joining a group or being declined deletes the request row, so "the row exists" is the same as "still pending".
    private async Task<HashSet<Guid>> FindPendingRequestNotificationsAsync(Guid currentUserId, List<Notification> items)
    {
        var pending = new HashSet<Guid>();

        var friendItems = items.Where(n => n.Type == NotificationType.FriendRequestReceived && n.FriendRequestId.HasValue).ToList();
        if (friendItems.Count > 0)
        {
            var requestIds = friendItems.Select(n => n.FriendRequestId!.Value).Distinct().ToList();
            var open = (await _friendRequests
                    .GetAll(r => requestIds.Contains(r.Id) && r.Status == FriendRequestStatus.Pending, asNoTracking: true)
                    .Select(r => r.Id)
                    .ToListAsync())
                .ToHashSet();
            foreach (var n in friendItems.Where(n => open.Contains(n.FriendRequestId!.Value)))
                pending.Add(n.Id);
        }

        var joinItems = items.Where(n => n.Type == NotificationType.GroupJoinRequestReceived && n.GroupId.HasValue).ToList();
        if (joinItems.Count > 0)
        {
            var groupIds = joinItems.Select(n => n.GroupId!.Value).Distinct().ToList();
            var open = (await _joinRequests
                    .GetAll(r => groupIds.Contains(r.GroupId), asNoTracking: true)
                    .Select(r => new { r.GroupId, r.UserId })
                    .ToListAsync())
                .Select(r => (r.GroupId, r.UserId))
                .ToHashSet();
            foreach (var n in joinItems.Where(n => open.Contains((n.GroupId!.Value, n.ActorId))))
                pending.Add(n.Id);
        }

        var inviteItems = items.Where(n => n.Type == NotificationType.GroupInviteReceived && n.GroupId.HasValue).ToList();
        if (inviteItems.Count > 0)
        {
            var groupIds = inviteItems.Select(n => n.GroupId!.Value).Distinct().ToList();
            var open = (await _invites
                    .GetAll(i => i.UserId == currentUserId && groupIds.Contains(i.GroupId), asNoTracking: true)
                    .Select(i => i.GroupId)
                    .ToListAsync())
                .ToHashSet();
            foreach (var n in inviteItems.Where(n => open.Contains(n.GroupId!.Value)))
                pending.Add(n.Id);
        }

        return pending;
    }

    // The admins speak as one: whoever pressed the button is not shown to the person whose content was removed.
    private static void HideActor(GetNotificationDto dto)
    {
        dto.ActorId = Guid.Empty;
        dto.ActorName = string.Empty;
        dto.ActorAvatarUrl = null;
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

        var pendingRequests = await FindPendingRequestNotificationsAsync(currentUserId, items);

        var dtos = _mapper.Map<List<GetNotificationDto>>(items);

        for (var i = 0; i < dtos.Count; i++)
        {
            dtos[i].IsPending = pendingRequests.Contains(items[i].Id);

            if (actors.TryGetValue(items[i].ActorId, out var actor))
            {
                dtos[i].ActorName = actor.FullName;
                dtos[i].ActorAvatarUrl = actor.AvatarUrl;
            }

            if (items[i].Type == NotificationType.ContentRemoved)
                HideActor(dtos[i]);

            if (items[i].GroupId.HasValue && groupNames.TryGetValue(items[i].GroupId!.Value, out var groupName))
                dtos[i].GroupName = groupName;

            if (items[i].Subject != null && items[i].Type == NotificationType.PostLiked)
                dtos[i].Reaction = items[i].Subject;
            else if (items[i].Subject != null && items[i].Type == NotificationType.MarketplacePriceDropped)
                dtos[i].ListingTitle = items[i].Subject;
            else if (items[i].Subject != null)
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
            ?? throw new NotFoundException(Loc.T("notification.notFound"));

        if (notification.RecipientId != currentUserId)
            throw new ForbiddenException(Loc.T("notification.notYours"));

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

    public async Task DeleteByPostsAsync(IReadOnlyCollection<Guid> postIds)
    {
        if (postIds.Count == 0)
            return;

        var related = await _notifications.GetAll(n => n.PostId != null && postIds.Contains(n.PostId.Value)).ToListAsync();
        if (related.Count == 0)
            return;

        foreach (var notification in related)
            _notifications.Delete(notification);

        await _notifications.SaveChangesAsync();
    }

    public async Task DeleteByGroupAsync(Guid groupId)
    {
        var related = await _notifications.GetAll(n => n.GroupId == groupId).ToListAsync();
        if (related.Count == 0)
            return;

        foreach (var notification in related)
            _notifications.Delete(notification);

        await _notifications.SaveChangesAsync();
    }

    public async Task DeleteByListingAsync(Guid listingId)
    {
        var related = await _notifications.GetAll(n => n.ListingId == listingId).ToListAsync();
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
