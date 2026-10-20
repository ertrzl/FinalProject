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
    CommentReplied = 26,            // to the author of a comment: someone answered it
    Mentioned = 27,                 // someone tagged you with @username in a post (PostId) or a comment (CommentId too)
    PostShared = 28,                // someone shared your post: PostId is THEIR post (the share), so the link opens it
    ContentRemoved = 29,            // an admin removed something of yours (post, comment, listing, event, group); Subject carries its title. The admin is not named
    StoryLiked = 30,                // someone liked your story (StoryId tells the likes of different stories apart; the story itself may be gone by then)
    GameInviteReceived = 31,        // a friend invited you to a game (GameId); the friend is the actor
    GameInviteAccepted = 32,        // to the inviter: the friend accepted the game invitation (GameId)
    StoryMentioned = 33,            // a friend tagged you in a story (StoryId)
    StoryReshared = 34,             // to the owner: the tagged friend added the story to their own (StoryId is the original story)
    MarketplacePriceDropped = 45,   // to everyone who saved a listing: its seller lowered the price (ListingId, Amount = new price, PreviousAmount = old one, Subject = title)
    EventShared = 40,               // to the organizer: someone shared the event to their feed (EventId; PostId is THEIR post, so the link can open it)
    GroupPostPinned = 35,           // to every member: an admin pinned a wall post as an announcement (PostId, GroupId); muted members get none
    GroupChatAdded = 55             // a friend added you to a group chat made on the Messages page (ConversationId); the friend is the actor
}
