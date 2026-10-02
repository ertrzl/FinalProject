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
    MarketplaceRatingReceived = 17  // to the seller; Notification.Amount carries the star count
}
