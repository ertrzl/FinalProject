namespace SocialNetworkPlatformProject.Domain.Enums;

// Matches notifications.html badge icons: person-plus (friend), heart (like), chat (comment), check (accepted)
public enum NotificationType
{
    FriendRequestReceived = 0,
    FriendRequestAccepted = 1,
    PostLiked = 2,
    CommentAdded = 3,
    CommentLiked = 4,
    GroupJoinRequestReceived = 5, // sent to every group admin
    GroupInviteReceived = 6,
    GroupMemberRemoved = 7, // kicked
    GroupRoleChanged = 8,
    GroupJoinRequestApproved = 9,
    GroupJoinRequestRejected = 10,
    MarketplaceOfferReceived = 11,  // to the seller: a buyer made an offer
    MarketplaceOfferCountered = 12, // to the other party: a counter-offer
    MarketplaceOfferAccepted = 13,
    MarketplaceOfferRejected = 14,
    MarketplaceOfferWithdrawn = 15,
    MarketplaceOfferClosed = 16,    // the listing was sold while the negotiation was open
    MarketplaceRatingReceived = 17, // to the seller; Notification.Amount carries the star count
    EventUpdated = 18,              // to everyone going/interested: the organizer changed the title, time or place
    EventCancelled = 19,            // the organizer deleted the event; Notification.Subject carries its title
    EventInviteReceived = 20,       // someone invited you to an event
    EventAnnouncement = 21,         // the organizer posted an announcement on an event you're going to / interested in
    EventJoined = 22,               // to the organizer: someone is going to the event
    EventCommentAdded = 23,         // to the organizer: someone commented on the event
    EventWaitlistPromoted = 24,     // to someone on the waiting list: a spot opened and they are now going
    EventReminder = 25,             // the event is about to start (to those going and the organizer)
    CommentReplied = 26             // to the author of a comment: someone answered it
}
